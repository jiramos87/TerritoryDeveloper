#!/usr/bin/env node
// Compute BlipPatchHash (FNV-1a 32-bit) for World Blip patch SO assets.
// Mirrors BlipPatchHash.Compute field order exactly.
// Run: node tools/scripts/compute-blip-hash.mjs

const FNV_OFFSET = 0x811C9DC5n;
const FNV_PRIME  = 0x01000193n;
const MASK32     = 0xFFFFFFFFn;

function feedInt(h, v) {
  // little-endian 4 bytes
  const buf = Buffer.allocUnsafe(4);
  buf.writeInt32LE(v, 0);
  for (const b of buf) {
    h = ((h ^ BigInt(b)) * FNV_PRIME) & MASK32;
  }
  return h;
}

function feedFloat(h, v) {
  const buf = Buffer.allocUnsafe(4);
  buf.writeFloatLE(v, 0);
  const i = buf.readInt32LE(0);
  return feedInt(h, i);
}

function feedBool(h, v) {
  h = ((h ^ BigInt(v ? 1 : 0)) * FNV_PRIME) & MASK32;
  return h;
}

function feedEnum(h, v) {
  return feedInt(h, v);
}

// BlipWaveform: Sine=0 Triangle=1 Square=2 Pulse=3 NoiseWhite=4
// BlipFilterKind: None=0 LowPass=1
// BlipEnvShape: Linear=0 Exponential=1

function computeHash(patch) {
  let h = FNV_OFFSET;

  // 1. oscillatorCount
  const oscCount = Math.min(patch.oscillators.length, 3);
  h = feedInt(h, oscCount);

  // 2. Per-oscillator
  for (let i = 0; i < oscCount; i++) {
    const osc = patch.oscillators[i];
    h = feedEnum(h, osc.waveform);
    h = feedFloat(h, osc.frequencyHz);
    h = feedFloat(h, osc.detuneCents);
    h = feedFloat(h, osc.pulseDuty);
    h = feedFloat(h, osc.gain);
  }

  // 3. Envelope
  h = feedFloat(h, patch.envelope.attackMs);
  h = feedEnum(h, patch.envelope.attackShape);
  h = feedFloat(h, patch.envelope.holdMs);
  h = feedFloat(h, patch.envelope.decayMs);
  h = feedEnum(h, patch.envelope.decayShape);
  h = feedFloat(h, patch.envelope.sustainLevel);
  h = feedFloat(h, patch.envelope.releaseMs);
  h = feedEnum(h, patch.envelope.releaseShape);

  // 4. Filter
  h = feedEnum(h, patch.filter.kind);
  h = feedFloat(h, patch.filter.cutoffHz);

  // 5. Variation
  h = feedInt(h, patch.variantCount);

  // 6. Jitter
  h = feedFloat(h, patch.pitchJitterCents);
  h = feedFloat(h, patch.gainJitterDb);
  h = feedFloat(h, patch.panJitter);

  // 7. Voice management
  h = feedInt(h, patch.voiceLimit);
  h = feedInt(h, patch.priority);
  h = feedFloat(h, patch.cooldownMs);

  // 8. Flags / bake params
  h = feedBool(h, patch.deterministic);
  h = feedFloat(h, patch.durationSeconds);
  h = feedBool(h, patch.useLutOscillators);

  // Cast to int32 (same as C# (int)h)
  const u = Number(h);
  return u > 0x7FFFFFFF ? u - 0x100000000 : u;
}

// --- Patch definitions ---

// NOTE: HighPass not in BlipFilterKind enum (only None=0, LowPass=1).
// ToolRoadTick oscillators[1] filter is per-oscillator concept but BlipPatch
// has a single patch-level filter. The noise transient HP is captured as
// filter on the patch level using kind=0 (None) since HighPass not in MVP enum.
// Per spec §2.2 non-goal 6 and §6 Decision Log: MVP = single-shot per-voice.
// The spec table shows HighPass 4000 Hz for ToolRoadTick — but enum only has
// None/LowPass. Use None (0) for filter kind; capture cutoffHz=4000 for doc.
// OnValidate will recompute hash correctly either way.

const patches = {
  BlipPatch_ToolRoadTick: {
    oscillators: [
      { waveform: 0, frequencyHz: 2500, detuneCents: 0, pulseDuty: 0, gain: 0.35 }, // sine
      { waveform: 4, frequencyHz: 0,    detuneCents: 0, pulseDuty: 0, gain: 0.15 }, // NoiseWhite
    ],
    envelope: { attackMs: 1, attackShape: 0, holdMs: 0, decayMs: 25, decayShape: 0, sustainLevel: 0, releaseMs: 5, releaseShape: 0 },
    filter: { kind: 0, cutoffHz: 4000 }, // HighPass not in enum; use None + cutoffHz
    variantCount: 4,
    pitchJitterCents: 138,
    gainJitterDb: 0,
    panJitter: 0,
    voiceLimit: 2,
    priority: 0,
    cooldownMs: 30,
    deterministic: false,
    durationSeconds: 1,
    useLutOscillators: false,
  },
  BlipPatch_ToolRoadComplete: {
    oscillators: [
      { waveform: 1, frequencyHz: 523, detuneCents: 0, pulseDuty: 0, gain: 0.4 }, // triangle
    ],
    envelope: { attackMs: 2, attackShape: 0, holdMs: 0, decayMs: 48, decayShape: 0, sustainLevel: 0, releaseMs: 10, releaseShape: 0 },
    filter: { kind: 0, cutoffHz: 0 },
    variantCount: 1,
    pitchJitterCents: 0,
    gainJitterDb: 0,
    panJitter: 0,
    voiceLimit: 1,
    priority: 0,
    cooldownMs: 0,
    deterministic: false,
    durationSeconds: 1,
    useLutOscillators: false,
  },
  BlipPatch_ToolBuildingPlace: {
    oscillators: [
      { waveform: 1, frequencyHz: 523, detuneCents: 0, pulseDuty: 0, gain: 0.45 }, // triangle
    ],
    envelope: { attackMs: 5, attackShape: 0, holdMs: 0, decayMs: 45, decayShape: 0, sustainLevel: 0, releaseMs: 10, releaseShape: 0 },
    filter: { kind: 0, cutoffHz: 0 },
    variantCount: 1,
    pitchJitterCents: 0,
    gainJitterDb: 0,
    panJitter: 0,
    voiceLimit: 1,
    priority: 0,
    cooldownMs: 0,
    deterministic: false,
    durationSeconds: 1,
    useLutOscillators: false,
  },
  BlipPatch_ToolBuildingDenied: {
    oscillators: [
      { waveform: 3, frequencyHz: 400, detuneCents: 0, pulseDuty: 0.2, gain: 0.5 }, // pulse
    ],
    envelope: { attackMs: 1, attackShape: 0, holdMs: 0, decayMs: 120, decayShape: 0, sustainLevel: 0, releaseMs: 10, releaseShape: 0 },
    filter: { kind: 1, cutoffHz: 1500 }, // LowPass
    variantCount: 1,
    pitchJitterCents: 0,
    gainJitterDb: 0,
    panJitter: 0,
    voiceLimit: 1,
    priority: 0,
    cooldownMs: 0,
    deterministic: false,
    durationSeconds: 1,
    useLutOscillators: false,
  },
  BlipPatch_WorldCellSelected: {
    oscillators: [
      { waveform: 0, frequencyHz: 800, detuneCents: 0, pulseDuty: 0, gain: 0.35 }, // sine
    ],
    envelope: { attackMs: 1, attackShape: 0, holdMs: 0, decayMs: 29, decayShape: 0, sustainLevel: 0, releaseMs: 5, releaseShape: 0 },
    filter: { kind: 0, cutoffHz: 0 },
    variantCount: 1,
    pitchJitterCents: 0,
    gainJitterDb: 0,
    panJitter: 0,
    voiceLimit: 1,
    priority: 0,
    cooldownMs: 80,
    deterministic: false,
    durationSeconds: 1,
    useLutOscillators: false,
  },
};

for (const [name, patch] of Object.entries(patches)) {
  const hash = computeHash(patch);
  console.log(`${name}: ${hash}`);
}
