import assert from 'node:assert/strict';
import test from 'node:test';
import { checkFrontendAudit } from '../check-frontend-audit.mjs';

const duringException = Date.parse('2026-11-18T01:00:00Z');
const picomatchNode = 'node_modules/picomatch';
const lockfile = { packages: { [picomatchNode]: { version: '4.0.7', dev: true } } };
const advisory = {
  name: 'picomatch',
  dependency: 'picomatch',
  severity: 'high',
  url: 'https://github.com/advisories/GHSA-fc5q-39xc-vjpf',
};

function finding(name, via, severity = 'high') {
  return { name, via, severity, isDirect: false, nodes: [`node_modules/${name}`] };
}

function reportWithPicomatch() {
  return {
    auditReportVersion: 2,
    vulnerabilities: {
      picomatch: finding('picomatch', [advisory]),
      fdir: finding('fdir', ['picomatch']),
      tinyglobby: finding('tinyglobby', ['fdir', 'picomatch']),
      vite: finding('vite', ['picomatch', 'tinyglobby']),
      vitest: finding('vitest', ['picomatch', 'tinyglobby', 'vite']),
      '@vitejs/plugin-react': finding('@vitejs/plugin-react', ['vite']),
    },
  };
}

test('allows the exact Picomatch advisory and its npm meta-vulnerabilities', () => {
  assert.equal(checkFrontendAudit(reportWithPicomatch(), lockfile, 1, duringException), 'picomatch');
});

test('rejects a different high advisory on Picomatch itself', () => {
  const report = reportWithPicomatch();
  report.vulnerabilities.picomatch.via.push({ ...advisory, url: 'https://example.test/other' });
  assert.equal(checkFrontendAudit(report, lockfile, 1, duringException), 'fail');
});

test('rejects a separate high advisory on a package also affected by Picomatch', () => {
  const report = reportWithPicomatch();
  report.vulnerabilities.vite.via.push({ ...advisory, name: 'vite', dependency: 'vite' });
  assert.equal(checkFrontendAudit(report, lockfile, 1, duringException), 'fail');
});

test('rejects critical advisories, including a severity increase for the known advisory', () => {
  const report = reportWithPicomatch();
  report.vulnerabilities.picomatch = finding('picomatch', [{ ...advisory, severity: 'critical' }], 'critical');
  assert.equal(checkFrontendAudit(report, lockfile, 1, duringException), 'fail');
});

test('requires the one existing transitive development installation of Picomatch 4.0.7', () => {
  for (const installed of [{ version: '4.0.7' }, { version: '4.0.6', dev: true }]) {
    const differentLockfile = { packages: { [picomatchNode]: installed } };
    assert.equal(checkFrontendAudit(reportWithPicomatch(), differentLockfile, 1, duringException), 'fail');
  }
  const report = reportWithPicomatch();
  report.vulnerabilities.picomatch.nodes.push('node_modules/vite/node_modules/picomatch');
  assert.equal(checkFrontendAudit(report, lockfile, 1, duringException), 'fail');
  report.vulnerabilities.picomatch.nodes.pop();
  report.vulnerabilities.picomatch.isDirect = true;
  assert.equal(checkFrontendAudit(report, lockfile, 1, duringException), 'fail');
});

test('enforces the expiration and accepts a clean audit after the exception expires', () => {
  const expiresAt = Date.parse('2026-12-02T03:00:00Z');
  assert.equal(checkFrontendAudit(reportWithPicomatch(), lockfile, 1, expiresAt), 'fail');
  const clean = { auditReportVersion: 2, vulnerabilities: {} };
  assert.equal(checkFrontendAudit(clean, lockfile, 0, expiresAt), 'pass');
});

test('retains the existing audit threshold for moderate findings', () => {
  const report = reportWithPicomatch();
  report.vulnerabilities.vite.via.push({
    ...advisory, name: 'vite', dependency: 'vite', severity: 'moderate', url: 'https://example.test/moderate',
  });
  assert.equal(checkFrontendAudit(report, lockfile, 1, duringException), 'picomatch');
});

test('fails on npm errors and unrecognized report or dependency paths', () => {
  assert.equal(checkFrontendAudit({ error: { code: 'E503' } }, lockfile, 1, duringException), 'fail');
  assert.equal(checkFrontendAudit(reportWithPicomatch(), lockfile, 2, duringException), 'fail');
  const report = reportWithPicomatch();
  report.vulnerabilities.vite.via.push('missing');
  assert.equal(checkFrontendAudit(report, lockfile, 1, duringException), 'fail');
  report.vulnerabilities.vite.via = ['vitest'];
  assert.equal(checkFrontendAudit(report, lockfile, 1, duringException), 'fail');
});
