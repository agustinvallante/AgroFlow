// Minimal, test-only interpreter for the exported chatbot workflow. It runs the
// versioned JSON (connections, IF conditions, Set assignments and n8n
// expressions) with injected doubles for HTTP, the AI agent and the webhook
// response, so the routing can be verified without n8n, Evolution, OpenAI or
// WhatsApp. It supports only the node types and options that Chatbot.json uses
// and fails loudly on anything else. Expressions are evaluated as JavaScript:
// use it only with the repository's own workflow and synthetic fixtures.

const SUPPORTED = new Set([
  'n8n-nodes-base.webhook', 'n8n-nodes-base.if', 'n8n-nodes-base.set', 'n8n-nodes-base.httpRequest',
  'n8n-nodes-base.respondToWebhook', '@n8n/n8n-nodes-langchain.agent',
]);
// Sub-nodes are attached to the agent through ai_* connections and never run on the main path.
const SUB_NODES = new Set([
  '@n8n/n8n-nodes-langchain.lmChatOpenAi', '@n8n/n8n-nodes-langchain.memoryBufferWindow',
  'n8n-nodes-base.httpRequestTool',
]);

export class SimulationError extends Error {}

export function simulate(workflow, { body, env, http, agent }) {
  const nodes = new Map(workflow.nodes.map(node => [node.name, node]));
  for (const node of workflow.nodes) {
    if (!SUPPORTED.has(node.type) && !SUB_NODES.has(node.type)) {
      throw new SimulationError(`Unsupported node type ${node.type} (${node.name})`);
    }
  }
  const outputs = new Map();
  const trace = [];
  const requests = [];
  let response = null;

  const context = current => ({
    $json: current,
    $env: env,
    $: name => {
      if (!outputs.has(name)) throw new SimulationError(`Expression references ${name} before it ran`);
      return { item: { json: outputs.get(name) } };
    },
  });

  const evaluate = (value, current) => {
    if (typeof value !== 'string' || !value.startsWith('=')) return value;
    const template = value.slice(1);
    const run = expression => {
      const scope = context(current);
      return new Function('$json', '$env', '$', `return (${expression});`)(scope.$json, scope.$env, scope.$);
    };
    const whole = /^\{\{([\s\S]*)\}\}$/.exec(template.trim());
    if (whole && !whole[1].includes('{{')) return run(whole[1]);
    return template.replace(/\{\{([\s\S]*?)\}\}/g, (_match, expression) => String(run(expression)));
  };

  const check = (condition, current) => {
    const left = evaluate(condition.leftValue, current);
    const right = evaluate(condition.rightValue, current);
    const { type, operation } = condition.operator;
    switch (`${type}:${operation}`) {
      case 'boolean:true': return left === true;
      case 'boolean:false': return left === false;
      case 'string:equals': return typeof left === 'string' && left === right;
      case 'string:notContains': return typeof left === 'string' && !left.includes(right);
      case 'number:equals': return typeof left === 'number' && left === Number(right);
      default: throw new SimulationError(`Unsupported IF operator ${type}:${operation}`);
    }
  };

  const handlers = {
    'n8n-nodes-base.webhook': () => [[{ body: structuredClone(body), headers: {} }]],

    'n8n-nodes-base.if': (node, current) => {
      const { conditions, combinator } = node.parameters.conditions;
      const results = conditions.map(condition => check(condition, current));
      const pass = combinator === 'or' ? results.some(Boolean) : results.every(Boolean);
      return pass ? [[current], []] : [[], [current]];
    },

    'n8n-nodes-base.set': (node, current) => {
      const json = {};
      for (const assignment of node.parameters.assignments.assignments) {
        const value = evaluate(assignment.value, current);
        json[assignment.name] = assignment.type === 'number' ? Number(value) : value;
      }
      return [[json]];
    },

    'n8n-nodes-base.respondToWebhook': (node, current) => {
      if (response) throw new SimulationError(`Second webhook response from ${node.name}`);
      const raw = evaluate(node.parameters.responseBody, current);
      response = { node: node.name, status: node.parameters.options?.responseCode ?? 200,
        body: typeof raw === 'string' ? JSON.parse(raw) : raw, atStep: trace.length };
      return [[current]];
    },

    'n8n-nodes-base.httpRequest': async (node, current) => {
      const parameters = node.parameters;
      const request = {
        node: node.name,
        method: parameters.method ?? 'GET',
        url: evaluate(parameters.url, current),
        credential: Object.values(node.credentials ?? {})[0]?.name ?? null,
        body: parameters.sendBody ? JSON.parse(evaluate(parameters.jsonBody, current)) : undefined,
      };
      requests.push(request);
      const options = parameters.options?.response?.response ?? {};
      let result;
      try {
        result = await http(request);
      } catch (error) {
        if (node.onError === 'continueErrorOutput') return [[], [{ error: error.message }]];
        throw error;
      }
      if (result.statusCode >= 400 && !options.neverError) {
        if (node.onError === 'continueErrorOutput') return [[], [{ error: `HTTP ${result.statusCode}` }]];
        throw new SimulationError(`${node.name} failed with HTTP ${result.statusCode}`);
      }
      const json = options.fullResponse
        ? { statusCode: result.statusCode, body: result.body ?? {}, headers: {} }
        : result.body ?? {};
      return [[json], []];
    },

    '@n8n/n8n-nodes-langchain.agent': async (node, current) => {
      const tools = Object.entries(workflow.connections)
        .filter(([, ports]) => ports.ai_tool?.some(group => group.some(edge => edge.node === node.name)))
        .map(([name]) => name);
      try {
        const output = await agent({ text: evaluate(node.parameters.text, current), tools });
        return [[{ output }], []];
      } catch (error) {
        if (node.onError === 'continueErrorOutput') return [[], [{ error: error.message }]];
        throw error;
      }
    },
  };

  const run = async () => {
    const start = workflow.nodes.find(node => node.type === 'n8n-nodes-base.webhook');
    const pending = [{ name: start.name, input: null }];
    while (pending.length) {
      const { name, input } = pending.shift();
      const node = nodes.get(name);
      if (node.disabled) continue;
      trace.push(name);
      if (trace.length > 200) throw new SimulationError('Execution did not terminate');
      const results = await handlers[node.type](node, input);
      const produced = results.find(items => items.length);
      if (produced) outputs.set(name, produced[0]);
      results.forEach((items, index) => {
        if (!items.length) return;
        for (const edge of workflow.connections[name]?.main?.[index] ?? []) {
          pending.push({ name: edge.node, input: items[0] });
        }
      });
    }
    return { trace, requests, response, outputs };
  };

  return run();
}
