import {drawSearchArray} from "./renderers.js";

function clamp(value, min, max) {
  return Math.max(min, Math.min(max, value));
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
  }

  attachUi(doc) {
    this.ui = {
      algorithmSelect: doc.getElementById("algorithmSelect"),
      targetInput: doc.getElementById("targetInput"),
      sizeInput: doc.getElementById("sizeInput"),
      speedInput: doc.getElementById("speedInput"),
      playPauseBtn: doc.getElementById("playPauseBtn"),
      stepBtn: doc.getElementById("stepBtn"),
      resetBtn: doc.getElementById("resetBtn"),
      newDataBtn: doc.getElementById("newDataBtn"),
      statusLine: doc.getElementById("statusLine"),
      comparisonLine: doc.getElementById("comparisonLine"),
      algorithmTitle: doc.getElementById("algorithmTitle"),
      algorithmDescription: doc.getElementById("algorithmDescription"),
      complexityBadge: doc.getElementById("complexityBadge"),
      pseudocodeList: doc.getElementById("pseudocodeList")
    };

    this.ui.algorithmSelect.addEventListener("change", () => this.selectAlgorithm(this.ui.algorithmSelect.value));

    this.ui.targetInput.addEventListener("change", () => {
      this.model.target = Number(this.ui.targetInput.value);
      this.reset();
    });

    this.ui.sizeInput.addEventListener("change", () => this.generateData());

    this.ui.newDataBtn.addEventListener("click", () => this.generateData());

    this.ui.resetBtn.addEventListener("click", () => this.reset());

    this.ui.playPauseBtn.addEventListener("click", () => {
      this.playing = !this.playing;
      this.ui.playPauseBtn.textContent = this.playing ? "Pause" : "Play";
    });

    this.ui.stepBtn.addEventListener("click", () => {
      this.playing = false;
      this.ui.playPauseBtn.textContent = "Play";
      this.step();
    });

    this.selectAlgorithm(this.ui.algorithmSelect.value);
    this.ui.playPauseBtn.textContent = "Play";
  }

  selectAlgorithm(id) {
    const factory = this.algorithmFactories[id];
    this.currentAlgorithm = factory();
    this.ui.algorithmTitle.textContent = this.currentAlgorithm.title;
    this.ui.algorithmDescription.textContent = this.currentAlgorithm.description;
    this.ui.complexityBadge.textContent = this.currentAlgorithm.complexity;
    this.generateData();
  }

  getSpeedIntervalMs() {
    const speed = Number(this.ui.speedInput.value);
    return 930 - speed * 105;
  }

  getCurrentSize() {
    return clamp(Number(this.ui.sizeInput.value), 8, 28);
  }

  getCurrentTarget() {
    const raw = Number(this.ui.targetInput.value);
    return Number.isFinite(raw) ? raw : 42;
  }

  generateData() {
    this.model = this.currentAlgorithm.createModel({
      size: this.getCurrentSize(),
      target: this.getCurrentTarget()
    });

    this.accumulatorMs = 0;
    this.playing = false;
    this.ui.playPauseBtn.textContent = "Play";
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
    this.renderInfo();
  }

  step() {
    if (!this.model) {
      return;
    }

    this.currentAlgorithm.step(this.model);
    this.renderInfo();
  }

  update(timestampMs) {
    if (this.lastTimestampMs === 0) {
      this.lastTimestampMs = timestampMs;
      return;
    }

    const delta = timestampMs - this.lastTimestampMs;
    this.lastTimestampMs = timestampMs;

    if (!this.model || !this.playing || this.model.done) {
      return;
    }

    this.accumulatorMs += delta;
    const intervalMs = this.getSpeedIntervalMs();

    while (this.accumulatorMs >= intervalMs && !this.model.done) {
      this.accumulatorMs -= intervalMs;
      this.step();
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
