import { drawSearchArray } from "./renderers.js";

const STORAGE_KEY = "fastdata.demo.preferences.v1";
const DEFAULT_SEED = 42;

function clamp(value, min, max) {
  return Math.max(min, Math.min(max, value));
}

function isInteractiveTarget(target) {
  if (!(target instanceof HTMLElement)) {
    return false;
  }

  const tag = target.tagName;
  return target.isContentEditable || tag === "INPUT" || tag === "SELECT" || tag === "TEXTAREA" || tag === "BUTTON";
}

export class VisualizationEngine {
  constructor(algorithmFactories) {
    this.algorithmFactories = algorithmFactories;
    this.currentAlgorithm = null;
    this.model = null;
    this.playing = false;
    this.accumulatorMs = 0;
    this.lastTimestampMs = 0;
    this.ui = null;
    this.prefs = {
      algorithm: "linear",
      speed: 4,
      datasetMode: "random",
      seed: DEFAULT_SEED
    };
  }

  attachUi(doc) {
    this.ui = {
      algorithmSelect: doc.getElementById("algorithmSelect"),
      targetInput: doc.getElementById("targetInput"),
      sizeInput: doc.getElementById("sizeInput"),
      speedInput: doc.getElementById("speedInput"),
      datasetModeInput: doc.getElementById("datasetModeInput"),
      seedInput: doc.getElementById("seedInput"),
      playPauseBtn: doc.getElementById("playPauseBtn"),
      stepBtn: doc.getElementById("stepBtn"),
      resetBtn: doc.getElementById("resetBtn"),
      statusLine: doc.getElementById("statusLine"),
      comparisonLine: doc.getElementById("comparisonLine"),
      algorithmTitle: doc.getElementById("algorithmTitle"),
      algorithmDescription: doc.getElementById("algorithmDescription"),
      complexityBadge: doc.getElementById("complexityBadge"),
      pseudocodeList: doc.getElementById("pseudocodeList")
    };

    this.loadPreferences();

    this.ui.algorithmSelect.addEventListener("change", () => {
      this.prefs.algorithm = this.ui.algorithmSelect.value;
      this.savePreferences();
      this.selectAlgorithm(this.ui.algorithmSelect.value);
    });

    this.ui.targetInput.addEventListener("change", () => this.reset());
    this.ui.sizeInput.addEventListener("change", () => this.generateData());

    this.ui.datasetModeInput.addEventListener("change", () => {
      this.prefs.datasetMode = this.getDatasetMode();
      this.savePreferences();
      this.generateData();
    });

    this.ui.seedInput.addEventListener("change", () => {
      this.prefs.seed = this.getCurrentSeed();
      this.savePreferences();
      this.generateData();
    });

    this.ui.speedInput.addEventListener("change", () => {
      this.prefs.speed = this.getCurrentSpeed();
      this.savePreferences();
    });

    this.ui.resetBtn.addEventListener("click", () => this.reset());
    this.ui.playPauseBtn.addEventListener("click", () => this.togglePlayPause());
    this.ui.stepBtn.addEventListener("click", () => this.stepOnce());

    doc.addEventListener("keydown", (event) => this.onKeyDown(event));

    this.selectAlgorithm(this.ui.algorithmSelect.value);
    this.ui.playPauseBtn.textContent = "Play";
  }

  loadPreferences() {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      if (!raw) {
        this.applyPreferences();
        return;
      }

      const parsed = JSON.parse(raw);
      if (typeof parsed.algorithm === "string") {
        this.prefs.algorithm = parsed.algorithm;
      }

      if (typeof parsed.datasetMode === "string") {
        this.prefs.datasetMode = parsed.datasetMode;
      }

      if (Number.isFinite(parsed.speed)) {
        this.prefs.speed = clamp(Number(parsed.speed), 1, 8);
      }

      if (Number.isFinite(parsed.seed)) {
        this.prefs.seed = Number(parsed.seed);
      }
    } catch {
      this.prefs.seed = DEFAULT_SEED;
    }

    this.applyPreferences();
  }

  applyPreferences() {
    this.ui.algorithmSelect.value = this.prefs.algorithm;
    if (!this.ui.algorithmSelect.value) {
      this.ui.algorithmSelect.value = "linear";
      this.prefs.algorithm = "linear";
    }

    this.ui.speedInput.value = String(clamp(this.prefs.speed, 1, 8));
    this.ui.datasetModeInput.value = this.prefs.datasetMode;
    if (!this.ui.datasetModeInput.value) {
      this.ui.datasetModeInput.value = "random";
      this.prefs.datasetMode = "random";
    }

    this.ui.seedInput.value = String(this.prefs.seed);
  }

  savePreferences() {
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(this.prefs));
    } catch {
      // Ignore local storage failures.
    }
  }

  onKeyDown(event) {
    if (isInteractiveTarget(event.target)) {
      return;
    }

    if (event.code === "Space") {
      event.preventDefault();
      this.togglePlayPause();
      return;
    }

    if (event.key === "n" || event.key === "N") {
      event.preventDefault();
      this.stepOnce();
      return;
    }

    if (event.key === "r" || event.key === "R") {
      event.preventDefault();
      if (!this.ui.resetBtn.disabled) {
        this.reset();
      }
      return;
    }

    if (event.key === "+" || event.key === "=" || event.code === "NumpadAdd") {
      event.preventDefault();
      this.adjustSpeed(1);
      return;
    }

    if (event.key === "-" || event.key === "_" || event.code === "NumpadSubtract") {
      event.preventDefault();
      this.adjustSpeed(-1);
    }
  }

  adjustSpeed(delta) {
    const next = clamp(this.getCurrentSpeed() + delta, 1, 8);
    this.ui.speedInput.value = String(next);
    this.prefs.speed = next;
    this.savePreferences();
  }

  togglePlayPause() {
    if (this.ui.playPauseBtn.disabled || !this.model) {
      return;
    }

    if (!this.playing) {
      this.setResetDisabled(false);
    }

    this.playing = !this.playing;
    this.ui.playPauseBtn.textContent = this.playing ? "Pause" : "Play";
  }

  stepOnce() {
    if (this.ui.stepBtn.disabled) {
      return;
    }

    this.setResetDisabled(false);
    this.playing = false;
    this.ui.playPauseBtn.textContent = "Play";
    this.step();
  }

  selectAlgorithm(id) {
    const factory = this.algorithmFactories[id];
    if (!factory) {
      this.ui.algorithmSelect.value = "linear";
      this.prefs.algorithm = "linear";
      this.savePreferences();
      this.selectAlgorithm("linear");
      return;
    }

    this.currentAlgorithm = factory();
    this.ui.algorithmTitle.textContent = this.currentAlgorithm.title;
    this.ui.algorithmDescription.textContent = this.currentAlgorithm.description;
    this.ui.complexityBadge.textContent = this.currentAlgorithm.complexity;
    this.generateData();
  }

  getSpeedIntervalMs() {
    return 930 - this.getCurrentSpeed() * 105;
  }

  getCurrentSpeed() {
    const raw = Number(this.ui.speedInput.value);
    const safe = Number.isFinite(raw) ? raw : 4;
    const normalized = clamp(safe, 1, 8);
    this.ui.speedInput.value = String(normalized);
    return normalized;
  }

  getCurrentSize() {
    const raw = Number(this.ui.sizeInput.value);
    const safe = Number.isFinite(raw) ? raw : 16;
    const normalized = clamp(safe, 8, 256);
    this.ui.sizeInput.value = String(normalized);
    return normalized;
  }

  getCurrentTarget() {
    const raw = Number(this.ui.targetInput.value);
    const safe = Number.isFinite(raw) ? raw : 42;
    this.ui.targetInput.value = String(safe);
    return safe;
  }

  getCurrentSeed() {
    const raw = Number(this.ui.seedInput.value);
    const safe = Number.isFinite(raw) ? Math.trunc(raw) : DEFAULT_SEED;
    this.ui.seedInput.value = String(safe);
    return safe;
  }

  getDatasetMode() {
    const mode = this.ui.datasetModeInput.value;
    if (mode === "uniform" || mode === "clustered" || mode === "nearlySorted") {
      return mode;
    }

    this.ui.datasetModeInput.value = "random";
    return "random";
  }

  generateData() {
    const options = {
      size: this.getCurrentSize(),
      target: this.getCurrentTarget(),
      seed: this.getCurrentSeed(),
      datasetMode: this.getDatasetMode()
    };

    this.prefs.seed = options.seed;
    this.prefs.datasetMode = options.datasetMode;
    this.prefs.speed = this.getCurrentSpeed();
    this.savePreferences();

    this.model = this.currentAlgorithm.createModel(options);
    this.accumulatorMs = 0;
    this.playing = false;
    this.ui.playPauseBtn.textContent = "Play";
    this.setRunControlsDisabled(false);
    this.setResetDisabled(true);
    this.renderInfo();
  }

  reset() {
    if (!this.model) {
      return;
    }

    this.model.target = this.getCurrentTarget();
    this.model = this.currentAlgorithm.resetModel(this.model);
    this.accumulatorMs = 0;
    this.playing = false;
    this.ui.playPauseBtn.textContent = "Play";
    this.setRunControlsDisabled(false);
    this.setResetDisabled(true);
    this.renderInfo();
  }

  step() {
    if (!this.model) {
      return;
    }

    this.currentAlgorithm.step(this.model);
    this.renderInfo();

    if (this.model.done) {
      this.completeRun();
    }
  }

  completeRun() {
    this.playing = false;
    this.ui.playPauseBtn.textContent = "Play";
    this.setRunControlsDisabled(true);
  }

  setRunControlsDisabled(disabled) {
    this.ui.playPauseBtn.disabled = disabled;
    this.ui.stepBtn.disabled = disabled;
  }

  setResetDisabled(disabled) {
    this.ui.resetBtn.disabled = disabled;
  }

  update(timestampMs) {
    if (this.lastTimestampMs === 0) {
      this.lastTimestampMs = timestampMs;
      return;
    }

    const delta = timestampMs - this.lastTimestampMs;
    this.lastTimestampMs = timestampMs;

    if (!this.model) {
      return;
    }

    if (this.model.done) {
      if (this.playing) {
        this.completeRun();
      }

      return;
    }

    if (!this.playing) {
      return;
    }

    this.accumulatorMs += delta;
    const intervalMs = this.getSpeedIntervalMs();

    while (this.accumulatorMs >= intervalMs && !this.model.done) {
      this.accumulatorMs -= intervalMs;
      this.step();
    }

    if (this.model.done && this.playing) {
      this.completeRun();
    }
  }

  renderInfo() {
    this.ui.statusLine.textContent = this.model.status;
    this.ui.comparisonLine.textContent = this.model.comparisonText;

    this.ui.pseudocodeList.innerHTML = "";
    for (let i = 0; i < this.currentAlgorithm.pseudocode.length; i += 1) {
      const line = document.createElement("li");
      line.textContent = this.currentAlgorithm.pseudocode[i];

      if (i === this.model.activeLine) {
        line.className = "active";
      }

      this.ui.pseudocodeList.appendChild(line);
    }
  }

  draw(p) {
    if (!this.model) {
      return;
    }

    drawSearchArray(p, this.model);
  }
}
