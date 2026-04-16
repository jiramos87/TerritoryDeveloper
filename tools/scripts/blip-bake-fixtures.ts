#!/usr/bin/env ts-node
/**
 * blip-bake-fixtures.ts — Bake variant-0 fixture JSONs for 10 MVP BlipId patches.
 *
 * Pure TypeScript port of BlipVoice.Render scalar loop (oscillator bank + AHDSR
 * envelope + one-pole LP filter).  Mirrors C# math exactly so the
 * `BlipGoldenFixtureTests` EditMode suite can re-render the same buffer and
 * assert sum-of-abs within 1e-6.
 *
 * Run: npx ts-node tools/scripts/blip-bake-fixtures.ts
 * Output: tools/fixtures/blip/{id}-v0.json  (10 files)
 *
 * Field order frozen — matches BlipPatchHash.Compute field walk in BlipPatch.cs.
 */

import { writeFileSync, mkdirSync } from 'fs';
import { resolve } from 'path';

// ---------------------------------------------------------------------------
// Enums (mirrors BlipPatchTypes.cs) — plain objects for ts-node compatibility
// ---------------------------------------------------------------------------

const BlipWaveform = {
  Sine:       0,
  Triangle:   1,
  Square:     2,
  Pulse:      3,
  NoiseWhite: 4,
} as const;
type BlipWaveform = (typeof BlipWaveform)[keyof typeof BlipWaveform];

const BlipFilterKind = {
  None:    0,
  LowPass: 1,
} as const;
type BlipFilterKind = (typeof BlipFilterKind)[keyof typeof BlipFilterKind];

const BlipEnvShape = {
  Linear:      0,
  Exponential: 1,
} as const;
type BlipEnvShape = (typeof BlipEnvShape)[keyof typeof BlipEnvShape];

const BlipEnvStage = {
  Idle:    0,
  Attack:  1,
  Hold:    2,
  Decay:   3,
  Sustain: 4,
  Release: 5,
} as const;
type BlipEnvStage = (typeof BlipEnvStage)[keyof typeof BlipEnvStage];

// ---------------------------------------------------------------------------
// Types (mirrors BlipPatchFlat / BlipPatch field layout)
// ---------------------------------------------------------------------------

interface BlipOscillatorDef {
  waveform: BlipWaveform;
  frequencyHz: number;
  detuneCents: number;
  pulseDuty: number;
  gain: number;
}

interface BlipEnvelopeDef {
  attackMs: number;
  attackShape: BlipEnvShape;
  holdMs: number;
  decayMs: number;
  decayShape: BlipEnvShape;
  sustainLevel: number;
  releaseMs: number;
  releaseShape: BlipEnvShape;
}

interface BlipFilterDef {
  kind: BlipFilterKind;
  cutoffHz: number;
}

interface BlipPatchDef {
  id: string;
  oscillators: BlipOscillatorDef[];
  envelope: BlipEnvelopeDef;
  filter: BlipFilterDef;
  variantCount: number;
  pitchJitterCents: number;
  gainJitterDb: number;
  panJitter: number;
  voiceLimit: number;
  priority: number;
  cooldownMs: number;
  deterministic: boolean;
  durationSeconds: number;
  useLutOscillators: boolean;
}

// ---------------------------------------------------------------------------
// Mutable voice state (mirrors BlipVoiceState.cs)
// ---------------------------------------------------------------------------

interface BlipVoiceState {
  phaseA: number;       // double [0..2π)
  phaseB: number;
  phaseC: number;
  envLevel: number;     // float [0..1]
  envStage: BlipEnvStage;
  samplesElapsed: number;
  filterZ1: number;     // float
  rngState: number;     // uint32 — stored as JS number, masked to uint32
  panOffset: number;    // float
}

function defaultVoiceState(): BlipVoiceState {
  return {
    phaseA: 0, phaseB: 0, phaseC: 0,
    envLevel: 0, envStage: BlipEnvStage.Idle, samplesElapsed: 0,
    filterZ1: 0, rngState: 0, panOffset: 0,
  };
}

// ---------------------------------------------------------------------------
// xorshift32 — mirrors C# in BlipVoice + BlipOscillatorBank
// ---------------------------------------------------------------------------

/** One xorshift32 step; returns updated rngState as uint32. */
function xorshift32(s: number): number {
  // Must use Math.imul / bit ops that stay within 32-bit range.
  // C#: rngState ^= rngState << 13; rngState ^= rngState >> 17; rngState ^= rngState << 5;
  s = s >>> 0; // ensure uint32
  s ^= (s << 13) >>> 0;
  s ^= (s >>> 17);       // logical right shift — unsigned
  s ^= (s << 5) >>> 0;
  return s >>> 0;
}

// ---------------------------------------------------------------------------
// Oscillator bank (mirrors BlipOscillatorBank.cs)
// ---------------------------------------------------------------------------

const TWO_PI = 2.0 * Math.PI;

function sampleOsc(
  osc: BlipOscillatorDef,
  sampleRate: number,
  phaseRef: { v: number },
  rngRef: { v: number },
): number {
  if (sampleRate <= 0) return 0;

  // detune: effective freq = freq * 2^(cents/1200)
  const pitchMult = Math.pow(2.0, osc.detuneCents / 1200.0);
  const effectiveFreq = osc.frequencyHz * pitchMult;

  // Phase advance in radians; wrap at 2π
  phaseRef.v += TWO_PI * effectiveFreq / sampleRate;
  if (phaseRef.v >= TWO_PI) phaseRef.v -= TWO_PI;

  const phase = phaseRef.v;

  switch (osc.waveform) {
    case BlipWaveform.Sine:
      return Math.sin(phase);

    case BlipWaveform.Triangle: {
      const p = phase / TWO_PI;
      return 4.0 * Math.abs(p - 0.5) - 1.0;
    }

    case BlipWaveform.Square: {
      const p = phase / TWO_PI;
      return p < 0.5 ? 1.0 : -1.0;
    }

    case BlipWaveform.Pulse: {
      const p = phase / TWO_PI;
      const duty = Math.max(0, Math.min(1, osc.pulseDuty));
      return p < duty ? 1.0 : -1.0;
    }

    case BlipWaveform.NoiseWhite: {
      // xorshift32 step on rngState; map to [-1, 1]
      rngRef.v = xorshift32(rngRef.v);
      // C#: (int)rngState * (1f / int.MaxValue)
      // int.MaxValue = 2147483647; cast uint to signed int via >>> 0 then |0
      const signed = rngRef.v | 0; // reinterpret as int32
      return signed * (1.0 / 2147483647.0);
    }

    default:
      return 0;
  }
}

// ---------------------------------------------------------------------------
// Envelope (mirrors BlipEnvelopeStepper.cs)
// ---------------------------------------------------------------------------

function msToSamples(ms: number, sampleRate: number): number {
  if (ms <= 0) return 0;
  const samples = Math.round(sampleRate * ms / 1000.0); // AwayFromZero — JS Math.round rounds .5 up
  return samples < 1 ? 1 : samples;
}

function budgetFor(stage: BlipEnvStage, env: BlipEnvelopeDef, sampleRate: number): number {
  switch (stage) {
    case BlipEnvStage.Idle:    return 0;
    case BlipEnvStage.Attack:  return msToSamples(env.attackMs, sampleRate);
    case BlipEnvStage.Hold:    return msToSamples(env.holdMs, sampleRate);
    case BlipEnvStage.Decay:   return msToSamples(env.decayMs, sampleRate);
    case BlipEnvStage.Sustain: return 0; // unbounded
    case BlipEnvStage.Release: return msToSamples(env.releaseMs, sampleRate);
    default:                   return 0;
  }
}

function nextStage(current: BlipEnvStage, env: BlipEnvelopeDef, sampleRate: number): BlipEnvStage {
  switch (current) {
    case BlipEnvStage.Idle:
      return BlipEnvStage.Attack;
    case BlipEnvStage.Attack:
      if (msToSamples(env.holdMs, sampleRate) === 0)
        return msToSamples(env.decayMs, sampleRate) === 0
          ? BlipEnvStage.Sustain
          : BlipEnvStage.Decay;
      return BlipEnvStage.Hold;
    case BlipEnvStage.Hold:
      return msToSamples(env.decayMs, sampleRate) === 0
        ? BlipEnvStage.Sustain
        : BlipEnvStage.Decay;
    case BlipEnvStage.Decay:
      return BlipEnvStage.Sustain;
    case BlipEnvStage.Sustain:
      return BlipEnvStage.Release;
    case BlipEnvStage.Release:
      return BlipEnvStage.Idle;
    default:
      return BlipEnvStage.Idle;
  }
}

function computeLevel(
  env: BlipEnvelopeDef,
  stage: BlipEnvStage,
  samplesElapsed: number,
  stageDurationSamples: number,
  releaseStartLevel: number,
): number {
  // Flat-level stages
  if (stage === BlipEnvStage.Idle)    return 0;
  if (stage === BlipEnvStage.Hold)    return 1;
  if (stage === BlipEnvStage.Sustain) return env.sustainLevel;

  // Ramping stages
  let start: number, target: number, shape: BlipEnvShape;
  switch (stage) {
    case BlipEnvStage.Attack:
      start  = 0; target = 1; shape = env.attackShape; break;
    case BlipEnvStage.Decay:
      start  = 1; target = env.sustainLevel; shape = env.decayShape; break;
    case BlipEnvStage.Release:
      start  = releaseStartLevel; target = 0; shape = env.releaseShape; break;
    default:
      return 0;
  }

  if (stageDurationSamples <= 0) return target;

  if (shape === BlipEnvShape.Linear) {
    let t = samplesElapsed / stageDurationSamples;
    if (t < 0) t = 0;
    if (t > 1) t = 1;
    return start + (target - start) * t;
  } else {
    // Exponential: target + (start - target) * exp(-t / tau), tau = stageDur/4
    const tau = stageDurationSamples / 4.0;
    return target + (start - target) * Math.exp(-samplesElapsed / tau);
  }
}

function advanceEnvelope(
  state: BlipVoiceState,
  env: BlipEnvelopeDef,
  durationSeconds: number,
  sampleRate: number,
  voiceElapsedSamples: number,
): void {
  // One-shot release trigger
  const oneShotEndSamples = msToSamples(durationSeconds * 1000, sampleRate);
  const releaseDue = oneShotEndSamples > 0
    && voiceElapsedSamples >= oneShotEndSamples
    && state.envStage !== BlipEnvStage.Release
    && state.envStage !== BlipEnvStage.Idle;

  if (releaseDue) {
    state.envStage = BlipEnvStage.Release;
    state.samplesElapsed = 0;
  }

  // Per-sample counter + stage transition
  state.samplesElapsed++;
  const budget = budgetFor(state.envStage, env, sampleRate);

  if (state.envStage !== BlipEnvStage.Sustain && state.samplesElapsed >= budget) {
    state.envStage = nextStage(state.envStage, env, sampleRate);
    state.samplesElapsed = 0;
  }
}

// ---------------------------------------------------------------------------
// Jitter sampler (mirrors BlipVoice.SampleJitter)
// ---------------------------------------------------------------------------

function sampleJitter(rngRef: { v: number }, range: number): number {
  if (range === 0) return 0;
  rngRef.v = xorshift32(rngRef.v);
  // C#: (rngState & 0x7FFFFFFF) * (1f / 0x80000000) → [0,1)
  // then (t * 2 - 1) * range → [-range, range)
  const t = (rngRef.v & 0x7FFFFFFF) * (1.0 / 0x80000000);
  return (t * 2.0 - 1.0) * range;
}

// ---------------------------------------------------------------------------
// Render — full patch render to Float32Array
// Mirrors BlipVoice.Render non-deterministic path with variantIndex=0 + initial rngState=0.
// ---------------------------------------------------------------------------

function renderPatch(patch: BlipPatchDef, sampleRate: number): Float32Array {
  const sampleCount = Math.round(patch.durationSeconds * sampleRate);
  const buffer = new Float32Array(sampleCount);
  const state  = defaultVoiceState();

  const oscCount = Math.min(patch.oscillators.length, 3);

  // --- Filter coefficient (pre-compute once) ---
  let alpha: number;
  if (patch.filter.kind === BlipFilterKind.LowPass) {
    alpha = 1.0 - Math.exp(-TWO_PI * patch.filter.cutoffHz / sampleRate);
    if (alpha < 0) alpha = 0;
    if (alpha > 1) alpha = 1;
  } else {
    alpha = 1.0;
  }

  // --- Stage sample budgets (pre-compute once) ---
  const attackSamples  = msToSamples(patch.envelope.attackMs,  sampleRate);
  const holdSamples    = msToSamples(patch.envelope.holdMs,    sampleRate);
  const decaySamples   = msToSamples(patch.envelope.decayMs,   sampleRate);
  const releaseSamples = msToSamples(patch.envelope.releaseMs, sampleRate);

  let releaseStartLevel = state.envLevel;

  // --- Non-deterministic path: seed from variantIndex=0, initial rngState=0 ---
  // C#: seed = (uint)(variantIndex * 0x9E3779B9) ^ state.rngState
  //     if (seed == 0u) seed = 0x9E3779B9u;
  // With variantIndex=0 + state.rngState=0 → seed=0 → guard fires → seed = 0x9E3779B9
  let seed = (Math.imul(0, 0x9E3779B9) ^ state.rngState) >>> 0;
  if (seed === 0) seed = 0x9E3779B9 >>> 0;
  state.rngState = seed;

  // Sample jitter scalars (one each per Render call)
  const rngRef = { v: state.rngState };

  const cents     = sampleJitter(rngRef, patch.pitchJitterCents);
  const db        = sampleJitter(rngRef, patch.gainJitterDb);
  const panOffset = sampleJitter(rngRef, patch.panJitter);
  state.rngState  = rngRef.v;

  const gainMult = Math.pow(10.0, db / 20.0); // db=0 → gainMult=1

  // Option B: fold pitchCents into per-slot local copies
  const loc: BlipOscillatorDef[] = [];
  for (let k = 0; k < 3; k++) {
    if (k < oscCount) {
      const src = patch.oscillators[k];
      loc.push({ ...src, detuneCents: src.detuneCents + cents });
    } else {
      loc.push({ waveform: BlipWaveform.Sine, frequencyHz: 0, detuneCents: 0, pulseDuty: 0, gain: 0 });
    }
  }

  state.panOffset = panOffset;

  // Phase refs (mutable boxes so oscillator can update)
  const phaseRef = [{ v: state.phaseA }, { v: state.phaseB }, { v: state.phaseC }];
  const rng      = { v: state.rngState };

  // --- Per-sample loop ---
  for (let i = 0; i < sampleCount; i++) {
    let oscSum = 0.0;
    if (oscCount > 0) oscSum += loc[0].gain * sampleOsc(loc[0], sampleRate, phaseRef[0], rng);
    if (oscCount > 1) oscSum += loc[1].gain * sampleOsc(loc[1], sampleRate, phaseRef[1], rng);
    if (oscCount > 2) oscSum += loc[2].gain * sampleOsc(loc[2], sampleRate, phaseRef[2], rng);

    const stageBefore = state.envStage;
    advanceEnvelope(state, patch.envelope, patch.durationSeconds, sampleRate, i);

    if (stageBefore !== BlipEnvStage.Release && state.envStage === BlipEnvStage.Release)
      releaseStartLevel = state.envLevel;

    let stageBudget: number;
    switch (state.envStage) {
      case BlipEnvStage.Attack:  stageBudget = attackSamples;  break;
      case BlipEnvStage.Hold:    stageBudget = holdSamples;    break;
      case BlipEnvStage.Decay:   stageBudget = decaySamples;   break;
      case BlipEnvStage.Release: stageBudget = releaseSamples; break;
      default:                   stageBudget = 0;              break;
    }

    state.envLevel = computeLevel(
      patch.envelope,
      state.envStage,
      state.samplesElapsed,
      stageBudget,
      releaseStartLevel,
    );

    const x = oscSum * state.envLevel * gainMult;
    state.filterZ1 += alpha * (x - state.filterZ1);
    buffer[i] = state.filterZ1;
  }

  return buffer;
}

// ---------------------------------------------------------------------------
// Fingerprint helpers
// ---------------------------------------------------------------------------

function sumAbsHash(buf: Float32Array): number {
  let acc = 0.0;
  for (let i = 0; i < buf.length; i++) {
    acc += Math.abs(buf[i]);
  }
  return acc;
}

function countZeroCrossings(buf: Float32Array): number {
  let count = 0;
  let prevSign = 0;
  for (let i = 0; i < buf.length; i++) {
    const s = buf[i];
    if (s === 0) continue; // skip exact zero
    const sign = s > 0 ? 1 : -1;
    if (prevSign !== 0 && sign !== prevSign) count++;
    prevSign = sign;
  }
  return count;
}

// ---------------------------------------------------------------------------
// FNV-1a 32-bit patchHash — mirrors BlipPatchHash.Compute field walk exactly.
// ---------------------------------------------------------------------------

const FNV_OFFSET = BigInt(0x811C9DC5);
const FNV_PRIME  = BigInt(0x01000193);
const MASK32     = BigInt(0xFFFFFFFF);

function feedInt(h: bigint, v: number): bigint {
  // little-endian 4 bytes — mirrors C# BitConverter.GetBytes(int) on LE machine
  const buf = Buffer.allocUnsafe(4);
  buf.writeInt32LE(v, 0);
  for (const b of buf) {
    h = ((h ^ BigInt(b)) * FNV_PRIME) & MASK32;
  }
  return h;
}

function feedFloat(h: bigint, v: number): bigint {
  // float32 bits → int32 → feedInt  (mirrors C# BitConverter.SingleToInt32Bits)
  const buf = Buffer.allocUnsafe(4);
  buf.writeFloatLE(v, 0);
  const i = buf.readInt32LE(0);
  return feedInt(h, i);
}

function feedBool(h: bigint, v: boolean): bigint {
  h = ((h ^ BigInt(v ? 1 : 0)) * FNV_PRIME) & MASK32;
  return h;
}

function feedEnum(h: bigint, v: number): bigint {
  return feedInt(h, v);
}

function computePatchHash(patch: BlipPatchDef): number {
  let h = FNV_OFFSET;

  const oscCount = Math.min(patch.oscillators.length, 3);
  h = feedInt(h, oscCount);

  for (let i = 0; i < oscCount; i++) {
    const osc = patch.oscillators[i];
    h = feedEnum(h, osc.waveform);
    h = feedFloat(h, osc.frequencyHz);
    h = feedFloat(h, osc.detuneCents);
    h = feedFloat(h, osc.pulseDuty);
    h = feedFloat(h, osc.gain);
  }

  const env = patch.envelope;
  h = feedFloat(h, env.attackMs);
  h = feedEnum(h, env.attackShape);
  h = feedFloat(h, env.holdMs);
  h = feedFloat(h, env.decayMs);
  h = feedEnum(h, env.decayShape);
  h = feedFloat(h, env.sustainLevel);
  h = feedFloat(h, env.releaseMs);
  h = feedEnum(h, env.releaseShape);

  h = feedEnum(h, patch.filter.kind);
  h = feedFloat(h, patch.filter.cutoffHz);

  h = feedInt(h, patch.variantCount);

  h = feedFloat(h, patch.pitchJitterCents);
  h = feedFloat(h, patch.gainJitterDb);
  h = feedFloat(h, patch.panJitter);

  h = feedInt(h, patch.voiceLimit);
  h = feedInt(h, patch.priority);
  h = feedFloat(h, patch.cooldownMs);

  h = feedBool(h, patch.deterministic);
  h = feedFloat(h, patch.durationSeconds);
  h = feedBool(h, patch.useLutOscillators);

  // Cast to int32 (same as C# (int)h)
  const u = Number(h);
  return u > 0x7FFFFFFF ? u - 0x100000000 : u;
}

// ---------------------------------------------------------------------------
// Patch recipe table — values sourced from SO assets under
// Assets/Audio/Blip/Patches/*.asset  (authoritative shipped values).
// BlipId → BlipPatch field map: SO is the product; §9 prose is design source.
// ---------------------------------------------------------------------------

const PATCHES: BlipPatchDef[] = [
  {
    id: 'UiButtonHover',
    oscillators: [
      { waveform: BlipWaveform.Triangle, frequencyHz: 2000, detuneCents: 0, pulseDuty: 0, gain: 0.3 },
    ],
    envelope: { attackMs: 5, attackShape: BlipEnvShape.Linear, holdMs: 0, decayMs: 30, decayShape: BlipEnvShape.Linear, sustainLevel: 0, releaseMs: 5, releaseShape: BlipEnvShape.Linear },
    filter: { kind: BlipFilterKind.LowPass, cutoffHz: 4000 },
    variantCount: 1, pitchJitterCents: 0, gainJitterDb: 0, panJitter: 0,
    voiceLimit: 1, priority: 0, cooldownMs: 120,
    deterministic: false, durationSeconds: 1, useLutOscillators: false,
  },
  {
    id: 'UiButtonClick',
    oscillators: [
      { waveform: BlipWaveform.Square, frequencyHz: 1000, detuneCents: 0, pulseDuty: 0, gain: 0.5 },
    ],
    envelope: { attackMs: 1, attackShape: BlipEnvShape.Linear, holdMs: 0, decayMs: 70, decayShape: BlipEnvShape.Linear, sustainLevel: 0, releaseMs: 10, releaseShape: BlipEnvShape.Linear },
    filter: { kind: BlipFilterKind.None, cutoffHz: 0 },
    variantCount: 1, pitchJitterCents: 0, gainJitterDb: 0, panJitter: 0,
    voiceLimit: 1, priority: 0, cooldownMs: 0,
    deterministic: false, durationSeconds: 1, useLutOscillators: false,
  },
  {
    id: 'ToolRoadTick',
    oscillators: [
      { waveform: BlipWaveform.Sine,       frequencyHz: 2500, detuneCents: 0, pulseDuty: 0, gain: 0.35 },
      { waveform: BlipWaveform.NoiseWhite, frequencyHz: 0,    detuneCents: 0, pulseDuty: 0, gain: 0.15 },
    ],
    envelope: { attackMs: 1, attackShape: BlipEnvShape.Linear, holdMs: 0, decayMs: 25, decayShape: BlipEnvShape.Linear, sustainLevel: 0, releaseMs: 5, releaseShape: BlipEnvShape.Linear },
    filter: { kind: BlipFilterKind.None, cutoffHz: 4000 },
    variantCount: 4, pitchJitterCents: 138, gainJitterDb: 0, panJitter: 0,
    voiceLimit: 2, priority: 0, cooldownMs: 30,
    deterministic: false, durationSeconds: 1, useLutOscillators: false,
  },
  {
    id: 'ToolRoadComplete',
    oscillators: [
      { waveform: BlipWaveform.Triangle, frequencyHz: 523, detuneCents: 0, pulseDuty: 0, gain: 0.4 },
    ],
    envelope: { attackMs: 2, attackShape: BlipEnvShape.Linear, holdMs: 0, decayMs: 48, decayShape: BlipEnvShape.Linear, sustainLevel: 0, releaseMs: 10, releaseShape: BlipEnvShape.Linear },
    filter: { kind: BlipFilterKind.None, cutoffHz: 0 },
    variantCount: 1, pitchJitterCents: 0, gainJitterDb: 0, panJitter: 0,
    voiceLimit: 1, priority: 0, cooldownMs: 0,
    deterministic: false, durationSeconds: 1, useLutOscillators: false,
  },
  {
    id: 'ToolBuildingPlace',
    oscillators: [
      { waveform: BlipWaveform.Triangle, frequencyHz: 523, detuneCents: 0, pulseDuty: 0, gain: 0.45 },
    ],
    envelope: { attackMs: 5, attackShape: BlipEnvShape.Linear, holdMs: 0, decayMs: 45, decayShape: BlipEnvShape.Linear, sustainLevel: 0, releaseMs: 10, releaseShape: BlipEnvShape.Linear },
    filter: { kind: BlipFilterKind.None, cutoffHz: 0 },
    variantCount: 1, pitchJitterCents: 0, gainJitterDb: 0, panJitter: 0,
    voiceLimit: 1, priority: 0, cooldownMs: 0,
    deterministic: false, durationSeconds: 1, useLutOscillators: false,
  },
  {
    id: 'ToolBuildingDenied',
    oscillators: [
      { waveform: BlipWaveform.Pulse, frequencyHz: 400, detuneCents: 0, pulseDuty: 0.2, gain: 0.5 },
    ],
    envelope: { attackMs: 1, attackShape: BlipEnvShape.Linear, holdMs: 0, decayMs: 120, decayShape: BlipEnvShape.Linear, sustainLevel: 0, releaseMs: 10, releaseShape: BlipEnvShape.Linear },
    filter: { kind: BlipFilterKind.LowPass, cutoffHz: 1500 },
    variantCount: 1, pitchJitterCents: 0, gainJitterDb: 0, panJitter: 0,
    voiceLimit: 1, priority: 0, cooldownMs: 0,
    deterministic: false, durationSeconds: 1, useLutOscillators: false,
  },
  {
    id: 'WorldCellSelected',
    oscillators: [
      { waveform: BlipWaveform.Sine, frequencyHz: 800, detuneCents: 0, pulseDuty: 0, gain: 0.35 },
    ],
    envelope: { attackMs: 1, attackShape: BlipEnvShape.Linear, holdMs: 0, decayMs: 29, decayShape: BlipEnvShape.Linear, sustainLevel: 0, releaseMs: 5, releaseShape: BlipEnvShape.Linear },
    filter: { kind: BlipFilterKind.None, cutoffHz: 0 },
    variantCount: 1, pitchJitterCents: 0, gainJitterDb: 0, panJitter: 0,
    voiceLimit: 1, priority: 0, cooldownMs: 80,
    deterministic: false, durationSeconds: 1, useLutOscillators: false,
  },
  {
    id: 'EcoMoneyEarned',
    oscillators: [
      { waveform: BlipWaveform.Sine, frequencyHz: 1319, detuneCents: 0, pulseDuty: 0, gain: 1 },
    ],
    envelope: { attackMs: 1, attackShape: BlipEnvShape.Linear, holdMs: 0, decayMs: 60, decayShape: BlipEnvShape.Linear, sustainLevel: 0, releaseMs: 1, releaseShape: BlipEnvShape.Linear },
    filter: { kind: BlipFilterKind.None, cutoffHz: 0 },
    variantCount: 1, pitchJitterCents: 0, gainJitterDb: 0, panJitter: 0,
    voiceLimit: 1, priority: 0, cooldownMs: 0,
    deterministic: false, durationSeconds: 1, useLutOscillators: false,
  },
  {
    id: 'EcoMoneySpent',
    oscillators: [
      { waveform: BlipWaveform.Triangle,   frequencyHz: 200, detuneCents: 0, pulseDuty: 0, gain: 0.6 },
      { waveform: BlipWaveform.NoiseWhite, frequencyHz: 0,   detuneCents: 0, pulseDuty: 0, gain: 0.2 },
    ],
    envelope: { attackMs: 1, attackShape: BlipEnvShape.Linear, holdMs: 0, decayMs: 80, decayShape: BlipEnvShape.Linear, sustainLevel: 0, releaseMs: 1, releaseShape: BlipEnvShape.Linear },
    filter: { kind: BlipFilterKind.None, cutoffHz: 0 },
    variantCount: 1, pitchJitterCents: 0, gainJitterDb: 0, panJitter: 0,
    voiceLimit: 1, priority: 0, cooldownMs: 0,
    deterministic: false, durationSeconds: 1, useLutOscillators: false,
  },
  {
    id: 'SysSaveGame',
    oscillators: [
      { waveform: BlipWaveform.Triangle, frequencyHz: 523, detuneCents: 0, pulseDuty: 0, gain: 1 },
      { waveform: BlipWaveform.Triangle, frequencyHz: 659, detuneCents: 0, pulseDuty: 0, gain: 1 },
      { waveform: BlipWaveform.Triangle, frequencyHz: 784, detuneCents: 0, pulseDuty: 0, gain: 1 },
    ],
    envelope: { attackMs: 2, attackShape: BlipEnvShape.Linear, holdMs: 0, decayMs: 58, decayShape: BlipEnvShape.Linear, sustainLevel: 0, releaseMs: 1, releaseShape: BlipEnvShape.Linear },
    filter: { kind: BlipFilterKind.None, cutoffHz: 0 },
    variantCount: 1, pitchJitterCents: 0, gainJitterDb: 0, panJitter: 0,
    voiceLimit: 1, priority: 0, cooldownMs: 2000,
    deterministic: false, durationSeconds: 1, useLutOscillators: false,
  },
];

// ---------------------------------------------------------------------------
// Main — emit 10 fixture JSONs
// ---------------------------------------------------------------------------

const SAMPLE_RATE = 48000;
// Resolve output path relative to repo root (two dirs up from tools/scripts/).
// Works whether ts-node runs in CJS or ESM mode: use process.cwd() as anchor
// when __dirname is unavailable; caller convention: run from repo root.
const SCRIPT_DIR  = typeof __dirname !== 'undefined'
  ? __dirname
  : resolve(process.cwd(), 'tools/scripts');
const OUT_DIR     = resolve(SCRIPT_DIR, '../fixtures/blip');

mkdirSync(OUT_DIR, { recursive: true });

for (const patch of PATCHES) {
  const buf          = renderPatch(patch, SAMPLE_RATE);
  const patchHash    = computePatchHash(patch);
  const sampleCount  = buf.length;
  const sab          = sumAbsHash(buf);
  const zc           = countZeroCrossings(buf);

  // Ordered JSON object — stable key order, 2-space indent, trailing newline.
  const obj = {
    id:            patch.id,
    variant:       0,
    patchHash,
    sampleRate:    SAMPLE_RATE,
    sampleCount,
    sumAbsHash:    sab,
    zeroCrossings: zc,
  };

  const keys: (keyof typeof obj)[] = ['id', 'variant', 'patchHash', 'sampleRate', 'sampleCount', 'sumAbsHash', 'zeroCrossings'];
  const ordered: Record<string, unknown> = {};
  for (const k of keys) ordered[k] = obj[k];

  const text = JSON.stringify(ordered, null, 2) + '\n';
  const outPath = resolve(OUT_DIR, `${patch.id}-v0.json`);
  writeFileSync(outPath, text, 'utf8');
  console.log(`Wrote ${patch.id}-v0.json  sampleCount=${sampleCount}  sumAbsHash=${sab.toFixed(6)}  zeroCrossings=${zc}  patchHash=${patchHash}`);
}

console.log(`\nBaked ${PATCHES.length} fixtures → ${OUT_DIR}`);
