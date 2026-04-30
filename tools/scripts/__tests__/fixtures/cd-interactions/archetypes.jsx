// Synthetic archetype JSX fixture — minimal Knob + ILed for parser tests.
const cx = (...parts) => parts.filter(Boolean).join(" ");

function Knob({ size = "md", tone = "primary", state, rotation = 0, label }) {
  const stateClass = state ? `knob--${state}` : "";
  return (
    <div className={cx("knob", `knob--${size}`, `knob--tone-${tone}`, stateClass)}>
      {label}
    </div>
  );
}

function ILed({ size = "md", tone = "primary", state, lit = false }) {
  return (
    <button className={cx("iled", `iled--${size}`, `iled--tone-${tone}`, lit && "iled--lit")} />
  );
}

Object.assign(window, { Knob, ILed });
