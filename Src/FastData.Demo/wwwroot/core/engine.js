import { drawSearchArray, drawSearchTree } from "./renderers.js";

const STORAGE_KEY = "fastdata.demo.preferences.v1";
const DEFAULT_SEED = 42;
const MIN_ZOOM = 0.5;
const MAX_ZOOM = 3;

function createDefaultCamera() {
  return {
    zoom: 1,
    panX: 0,
    panY: 0
  };
}

function normalizeCamera(value) {
  if (!value || typeof value !== "object") {
    return createDefaultCamera();
  }

  return {
    zoom: clamp(Number(value.zoom), MIN_ZOOM, MAX_ZOOM),
    panX: Number(value.panX),
    panY: Number(value.panY)
  };
}

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
    this.canvas = null;
    this.draggingCamera = false;
    this.lastPointerPosition = null;
    this.ui = null;
    this.prefs = {
      algorithm: "linear",
      viewMode: "array",
      speed: 4,
      datasetMode: "random",
      seed: DEFAULT_SEED,
      cameraByView: {
        array: createDefaultCamera(),
        tree: createDefaultCamera()
      }
    };
  }

  attachUi(doc) {
    this.ui = {
      algorithmSelect: doc.getElementById("algorithmSelect"),
      viewModeInput: doc.getElementById("viewModeInput"),
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
      boundsLine: doc.getElementById("boundsLine"),
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

    this.ui.viewModeInput.addEventListener("change", () => {
      this.prefs.viewMode = this.getCurrentViewMode();
      if (this.prefs.viewMode === "tree" && !this.algorithmSupportsTreeView()) {
        this.prefs.viewMode = "array";
      }

      this.ui.viewModeInput.value = this.prefs.viewMode;
      this.savePreferences();
      this.renderInfo();
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

  attachCanvas(canvas) {
    this.canvas = canvas;

    this.canvas.addEventListener("wheel", (event) => this.onCanvasWheel(event), { passive: false });
    this.canvas.addEventListener("pointerdown", (event) => this.onCanvasPointerDown(event));
    this.canvas.addEventListener("pointermove", (event) => this.onCanvasPointerMove(event));
    this.canvas.addEventListener("pointerup", (event) => this.onCanvasPointerUp(event));
    this.canvas.addEventListener("pointercancel", (event) => this.onCanvasPointerUp(event));
    this.canvas.addEventListener("dblclick", (event) => {
      event.preventDefault();
      this.resetView();
    });
  }

  onCanvasWheel(event) {
    if (!this.canvas) {
      return;
    }

    event.preventDefault();

    const pointer = this.getPointerPosition(event);
    if (!pointer) {
      return;
    }

    const factor = event.deltaY < 0 ? 1.1 : 1 / 1.1;
    this.zoomAt(pointer.x, pointer.y, factor);
    this.savePreferences();
  }

  onCanvasPointerDown(event) {
    if (!this.canvas || event.button !== 0) {
      return;
    }

    const pointer = this.getPointerPosition(event);
    if (!pointer) {
      return;
    }

    this.draggingCamera = true;
    this.lastPointerPosition = pointer;
    this.canvas.classList.add("is-panning");
    this.canvas.setPointerCapture(event.pointerId);
    event.preventDefault();
  }

  onCanvasPointerMove(event) {
    if (!this.canvas || !this.draggingCamera) {
      return;
    }

    const pointer = this.getPointerPosition(event);
    if (!pointer || !this.lastPointerPosition) {
      return;
    }

    const dx = pointer.x - this.lastPointerPosition.x;
    const dy = pointer.y - this.lastPointerPosition.y;
    this.lastPointerPosition = pointer;
    this.panBy(dx, dy);
    event.preventDefault();
  }

  onCanvasPointerUp(event) {
    if (!this.canvas) {
      return;
    }

    this.draggingCamera = false;
    this.lastPointerPosition = null;
    this.canvas.classList.remove("is-panning");
    this.savePreferences();

    if (this.canvas.hasPointerCapture(event.pointerId)) {
      this.canvas.releasePointerCapture(event.pointerId);
    }
  }

  getPointerPosition(event) {
    if (!this.canvas) {
      return null;
    }

    const rect = this.canvas.getBoundingClientRect();
    return {
      x: event.clientX - rect.left,
      y: event.clientY - rect.top
    };
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

      if (typeof parsed.viewMode === "string") {
        this.prefs.viewMode = parsed.viewMode;
      }

      if (parsed.cameraByView && typeof parsed.cameraByView === "object") {
        this.prefs.cameraByView.array = normalizeCamera(parsed.cameraByView.array);
        this.prefs.cameraByView.tree = normalizeCamera(parsed.cameraByView.tree);
      }

      this.prefs.speed = clamp(Number(parsed.speed), 1, 8);
      this.prefs.seed = Number(parsed.seed);
    } catch {
      this.prefs.seed = DEFAULT_SEED;
    }

    this.applyPreferences();
  }

  applyPreferences() {
    this.prefs.cameraByView.array = normalizeCamera(this.prefs.cameraByView.array);
    this.prefs.cameraByView.tree = normalizeCamera(this.prefs.cameraByView.tree);

    this.ui.algorithmSelect.value = this.prefs.algorithm;
    if (!this.ui.algorithmSelect.value) {
      this.ui.algorithmSelect.value = "linear";
      this.prefs.algorithm = "linear";
    }

    this.ui.speedInput.value = String(clamp(this.prefs.speed, 1, 8));
    this.ui.viewModeInput.value = this.prefs.viewMode;
    if (!this.ui.viewModeInput.value) {
      this.ui.viewModeInput.value = "array";
      this.prefs.viewMode = "array";
    }

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

    if (event.key === "0") {
      event.preventDefault();
      this.resetView();
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
    this.updateViewModeAvailability();
    this.generateData();
  }

  algorithmSupportsTreeView() {
    return Boolean(this.currentAlgorithm && this.currentAlgorithm.supportsTreeView);
  }

  updateViewModeAvailability() {
    const supportsTree = this.algorithmSupportsTreeView();
    const treeOption = this.ui.viewModeInput.querySelector('option[value="tree"]');

    if (treeOption) {
      treeOption.disabled = !supportsTree;
    }

    if (!supportsTree) {
      this.prefs.viewMode = "array";
      this.ui.viewModeInput.value = "array";
      this.savePreferences();
      return;
    }

    const currentMode = this.getCurrentViewMode();
    this.prefs.viewMode = currentMode;
    this.ui.viewModeInput.value = currentMode;
  }

  getSpeedIntervalMs() {
    return 930 - this.getCurrentSpeed() * 105;
  }

  getCurrentSpeed() {
    const raw = Number(this.ui.speedInput.value);
    const normalized = clamp(raw, 1, 8);
    this.ui.speedInput.value = String(normalized);
    return normalized;
  }

  getCurrentViewMode() {
    if (!this.ui || !this.ui.viewModeInput) {
      return this.prefs.viewMode === "tree" ? "tree" : "array";
    }

    const mode = this.ui.viewModeInput.value;
    if (mode === "tree") {
      return "tree";
    }

    this.ui.viewModeInput.value = "array";
    return "array";
  }

  getEffectiveViewMode() {
    const mode = this.getCurrentViewMode();
    if (mode === "tree" && this.algorithmSupportsTreeView()) {
      return "tree";
    }

    return "array";
  }

  getActiveCamera() {
    const mode = this.getEffectiveViewMode();
    if (mode === "tree") {
      return this.prefs.cameraByView.tree;
    }

    return this.prefs.cameraByView.array;
  }

  zoomBy(factor) {
    if (factor === 0 || !this.canvas) {
      return;
    }

    const rect = this.canvas.getBoundingClientRect();
    this.zoomAt(rect.width * 0.5, rect.height * 0.5, factor);
    this.savePreferences();
  }

  zoomAt(screenX, screenY, factor) {
    const camera = this.getActiveCamera();
    const oldZoom = camera.zoom;
    const newZoom = clamp(oldZoom * factor, MIN_ZOOM, MAX_ZOOM);

    if (newZoom === oldZoom) {
      return;
    }

    const worldX = (screenX - camera.panX) / oldZoom;
    const worldY = (screenY - camera.panY) / oldZoom;

    camera.zoom = newZoom;
    camera.panX = screenX - worldX * newZoom;
    camera.panY = screenY - worldY * newZoom;
  }

  panBy(dx, dy) {
    const camera = this.getActiveCamera();
    camera.panX += dx;
    camera.panY += dy;
  }

  resetView() {
    const camera = this.getActiveCamera();
    camera.zoom = 1;
    camera.panX = 0;
    camera.panY = 0;
    this.savePreferences();
  }

  getCurrentSize() {
    const raw = Number(this.ui.sizeInput.value);
    const normalized = clamp(raw, 8, 256);
    this.ui.sizeInput.value = String(normalized);
    return normalized;
  }

  getCurrentTarget() {
    const raw = Number(this.ui.targetInput.value);
    this.ui.targetInput.value = String(raw);
    return raw;
  }

  getCurrentSeed() {
    const raw = Number(this.ui.seedInput.value);
    const normalized = Math.trunc(raw);
    this.ui.seedInput.value = String(normalized);
    return normalized;
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

    const boundsText = this.getBoundsText();
    this.ui.boundsLine.textContent = boundsText ?? "";
    this.ui.boundsLine.hidden = boundsText === null;

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

  getBoundsText() {
    if (this.model.comparisons <= 0) {
      return null;
    }

    if (this.model.low === undefined || this.model.high === undefined) {
      return null;
    }

    if (!Array.isArray(this.model.data) || this.model.data.length === 0 || this.model.low > this.model.high) {
      return null;
    }

    const last = this.model.data.length - 1;
    const low = clamp(this.model.low, 0, last);
    const high = clamp(this.model.high, 0, last);
    const minValue = this.model.data[low];
    const maxValue = this.model.data[high];

    let midText = "-";
    if (this.model.mid !== undefined && this.model.mid !== null) {
      const midIndex = clamp(this.model.mid, 0, last);
      midText = `${this.model.data[midIndex]} (i=${midIndex})`;
    }

    return `Min: ${minValue}  Mid: ${midText}  Max: ${maxValue}`;
  }

  draw(p) {
    if (!this.model) {
      return;
    }

    const mode = this.getEffectiveViewMode();
    const camera = this.getActiveCamera();

    p.push();
    p.translate(camera.panX, camera.panY);
    p.scale(camera.zoom);

    if (mode === "tree") {
      drawSearchTree(p, this.model, this.currentAlgorithm.id);
    } else {
      drawSearchArray(p, this.model);
    }

    p.pop();
  }
}
