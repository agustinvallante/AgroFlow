import { readFileSync } from 'node:fs';
import { pathToFileURL } from 'node:url';

const advisory = 'https://github.com/advisories/GHSA-fc5q-39xc-vjpf';
const expiresAt = '2026-12-02T03:00:00Z';
const picomatchPath = 'node_modules/picomatch';
const severities = new Set(['info', 'low', 'moderate', 'high', 'critical']);
const isBlocking = (severity) => severity === 'high' || severity === 'critical';

function tracesToPicomatch(name, findings, path = new Set()) {
  const { severity, via } = findings[name];
  if (severity !== 'high' || path.has(name)) return false;

  const causes = via.filter((cause) => isBlocking(
    typeof cause === 'string' ? findings[cause].severity : cause.severity,
  ));
  const nextPath = new Set(path).add(name);

  return causes.length > 0 && causes.every((cause) => {
    if (typeof cause === 'string') return tracesToPicomatch(cause, findings, nextPath);
    return name === 'picomatch' && cause.name === 'picomatch' && cause.dependency === 'picomatch'
      && cause.url === advisory && cause.severity === 'high';
  });
}

export function checkFrontendAudit(report, lockfile, npmExit, now = Date.now()) {
  const findings = report?.vulnerabilities;
  if (report?.auditReportVersion !== 2 || report.error || !findings
      || typeof findings !== 'object' || Array.isArray(findings)) return 'fail';

  const entries = Object.entries(findings);
  for (const [name, finding] of entries) {
    if (finding?.name !== name || !severities.has(finding.severity)
        || !Array.isArray(finding.via) || finding.via.length === 0) return 'fail';
    for (const cause of finding.via) {
      if (typeof cause === 'string') {
        if (!Object.hasOwn(findings, cause)) return 'fail';
      } else if (typeof cause?.url !== 'string' || !severities.has(cause.severity)
          || (isBlocking(cause.severity) && !isBlocking(finding.severity))) {
        return 'fail';
      }
    }
  }

  const blockers = entries.filter(([, finding]) => isBlocking(finding.severity));
  if (npmExit === 0) return blockers.length === 0 ? 'pass' : 'fail';
  if (npmExit !== 1 || now >= Date.parse(expiresAt) || blockers.length === 0) return 'fail';

  const installed = lockfile?.packages?.[picomatchPath];
  const picomatch = findings.picomatch;
  if (installed?.version !== '4.0.7' || installed.dev !== true || picomatch?.isDirect !== false
      || !Array.isArray(picomatch.nodes) || picomatch.nodes.length !== 1
      || picomatch.nodes[0] !== picomatchPath) return 'fail';

  return blockers.every(([name]) => tracesToPicomatch(name, findings)) ? 'picomatch' : 'fail';
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  const [, , reportPath, npmExit] = process.argv;
  try {
    const report = JSON.parse(readFileSync(reportPath, 'utf8'));
    const lockfile = JSON.parse(readFileSync('package-lock.json', 'utf8'));
    const result = checkFrontendAudit(report, lockfile, Number(npmExit));
    if (result === 'picomatch') {
      console.log(`::warning::Temporary exception for ${advisory} on transitive dev dependency picomatch@4.0.7 (expires ${expiresAt}).`);
    } else if (result === 'fail') {
      console.error('Frontend audit failed. See the npm audit report above.');
      process.exitCode = 1;
    }
  } catch (error) {
    console.error(`Cannot evaluate the frontend audit report: ${error.message}`);
    process.exitCode = 1;
  }
}
