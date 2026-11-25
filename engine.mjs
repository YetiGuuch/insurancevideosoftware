import {createHash} from 'node:crypto';
import {spawn, execFile} from 'node:child_process';
import {createWriteStream, existsSync} from 'node:fs';
import {mkdir, rename, rm} from 'node:fs/promises';
import {createServer} from 'node:net';
import {join} from 'node:path';
import {Readable, Transform} from 'node:stream';
import {pipeline} from 'node:stream/promises';
import {promisify} from 'node:util';

const version = '0.40.18';
const archiveName = 'ollama-windows-amd64.zip';
const sha256 = 'ca97292ac1e9acc77fcf473bb2741fa6acd12e3798f261b419680068b7cc4c16';
const root = process.env.VIDEO_REVIEW_DATA;
const runtime = root && join(root, 'ollama-' + version);
const exe = runtime && join(runtime, 'ollama.exe');
const exec = promisify(execFile);
const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));
export const model = 'qwen3-vl:4b';

let child;
let endpoint = process.env.VIDEO_REVIEW_OLLAMA;
let starting;
let setup = {running: false, ready: false, message: 'Download the local engine and model once (about 5 GB).'};
const notify = message => { setup = {running: true, ready: false, message}; };

async function downloadRuntime() {
  if (existsSync(exe)) return;
  await mkdir(root, {recursive: true});
  const archive = join(root, archiveName + '.part');
  const staging = join(root, 'ollama-staging');
  await rm(staging, {recursive: true, force: true});
  await mkdir(staging);
  try {
    notify('Downloading the local engine…');
    const response = await fetch('https://github.com/ollama/ollama/releases/download/v' + version + '/' + archiveName, {
      signal: AbortSignal.timeout(7200000)
    });
    if (!response.ok || !response.body) throw new Error('Engine download failed (HTTP ' + response.status + ').');
    const total = Number(response.headers.get('content-length'));
    const hash = createHash('sha256');
    let bytes = 0;
    let updateAt = 0;
    const progress = new Transform({
      transform(chunk, encoding, callback) {
        hash.update(chunk);
        bytes += chunk.length;
        if (Date.now() >= updateAt) {
          const amount = total ? Math.floor(bytes * 100 / total) + '%' : Math.floor(bytes / 1048576) + ' MB';
          notify('Downloading the local engine… ' + amount);
          updateAt = Date.now() + 750;
        }
        callback(null, chunk);
      }
    });
    await pipeline(Readable.fromWeb(response.body), progress, createWriteStream(archive));
    if (hash.digest('hex') !== sha256) throw new Error('Engine download checksum did not match. Try again.');
    notify('Extracting the local engine…');
    await exec(join(process.env.SystemRoot, 'System32', 'tar.exe'), ['-xf', archive, '-C', staging], {windowsHide: true});
    if (!existsSync(join(staging, 'ollama.exe'))) throw new Error('The engine archive did not contain ollama.exe.');
    await rename(staging, runtime);
  } finally {
    await rm(archive, {force: true});
    await rm(staging, {recursive: true, force: true});
  }
}

function freePort() {
  return new Promise((resolve, reject) => {
    const socket = createServer();
    socket.once('error', reject);
    socket.listen(0, '127.0.0.1', () => {
      const {port} = socket.address();
      socket.close(error => error ? reject(error) : resolve(port));
    });
  });
}

async function startEngine() {
  if (endpoint) return endpoint;
  if (!exe || !existsSync(exe)) return null;
  if (starting) return starting;
  starting = (async () => {
    const host = '127.0.0.1:' + await freePort();
    const url = 'http://' + host;
    const instance = spawn(exe, ['serve'], {
      cwd: runtime, windowsHide: true, stdio: 'ignore',
      env: {...process.env, OLLAMA_HOST: host, OLLAMA_MODELS: join(root, 'models'), OLLAMA_NO_CLOUD: '1'}
    });
    child = instance;
    let failure;
    instance.on('error', error => { failure = error; });
    instance.on('exit', () => {
      failure = new Error('The local engine exited. Check available memory and try reopening the app.');
      if (child === instance) {
        child = undefined;
        endpoint = undefined;
      }
    });
    try {
      for (let attempt = 0; attempt < 80; attempt++) {
        if (failure) throw failure;
        try {
          const response = await fetch(url + '/api/version', {signal: AbortSignal.timeout(1000)});
          if (response.ok && (await response.json()).version === version) {
            endpoint = url;
            return url;
          }
        } catch {}
        await sleep(350);
      }
      throw new Error('The local engine did not start.');
    } catch (error) {
      instance.kill();
      throw error;
    }
  })();
  try {
    return await starting;
  } finally {
    starting = undefined;
  }
}

export async function readiness(signal) {
  const url = await startEngine();
  if (!url) return {ready: false, message: setup.message};
  const response = await fetch(url + '/api/tags', {signal});
  if (!response.ok) throw new Error('Could not check the local model.');
  const {models} = await response.json();
  return models?.some(item => item.name === model) ?
    {ready: true, model} : {ready: false, message: setup.message};
}

async function install() {
  if (!root) throw new Error('Start the Windows executable to set up the local engine.');
  await downloadRuntime();
  notify('Starting the local engine…');
  const url = await startEngine();
  if (!(await readiness(AbortSignal.timeout(10000))).ready) {
    notify('Downloading the vision model…');
    const response = await fetch(url + '/api/pull', {
      method: 'POST', signal: AbortSignal.timeout(7200000),
      headers: {'Content-Type': 'application/json'},
      body: JSON.stringify({model, stream: true})
    });
    if (!response.ok || !response.body) throw new Error('Model download failed (HTTP ' + response.status + ').');
    let success = false;
    const lines = (await import('node:readline')).createInterface({input: Readable.fromWeb(response.body)});
    for await (const line of lines) {
      if (!line.trim()) continue;
      const event = JSON.parse(line);
      if (event.error) throw new Error(event.error);
      if (event.status === 'success') success = true;
      const amount = event.total ? ' ' + Math.floor((event.completed || 0) * 100 / event.total) + '% of current file' : '';
      notify('Downloading the vision model…' + amount);
    }
    if (!success) throw new Error('The model download ended before it completed. Try again.');
  }
  setup = {running: false, ready: true, message: 'Analysis engine ready.'};
}

export function beginSetup() {
  if (setup.running) return setup;
  notify('Preparing first-time setup…');
  install().catch(error => {
    setup = {running: false, ready: false, message: error.message};
  });
  return setup;
}

export function setupStatus() { return setup; }
export async function engineUrl() { return startEngine(); }
export function stopEngine() { child?.kill(); }
