const STORAGE_KEY = "fastdata.demo.preferences.v1";
const DEFAULT_SEED = 42;
const MIN_ZOOM = 0.5;
const MAX_ZOOM = 3;

function clamp(value, min, max) {
  return Math.max(min, Math.min(max, value));
}

export function createDefaultCamera() {
  return {
    zoom: 1,
    panX: 0,
    panY: 0
  };
}

export function normalizeCamera(value) {
  if (!value || typeof value !== "object") {
    return createDefaultCamera();
  }

  return {
    zoom: clamp(Number(value.zoom), MIN_ZOOM, MAX_ZOOM),
    panX: Number(value.panX),
    panY: Number(value.panY)
  };
}

export function createDefaultPreferences() {
  return {
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

function normalizePreferences(input) {
  const defaults = createDefaultPreferences();
  const parsed = input && typeof input === "object" ? input : {};
  const cameraByView = parsed.cameraByView && typeof parsed.cameraByView === "object"
    ? parsed.cameraByView
    : {};

  return {
    algorithm: typeof parsed.algorithm === "string" ? parsed.algorithm : defaults.algorithm,
    viewMode: typeof parsed.viewMode === "string" ? parsed.viewMode : defaults.viewMode,
    speed: clamp(Number(parsed.speed ?? defaults.speed), 1, 8),
    datasetMode: typeof parsed.datasetMode === "string" ? parsed.datasetMode : defaults.datasetMode,
    seed: Number(parsed.seed ?? defaults.seed),
    cameraByView: {
      array: normalizeCamera(cameraByView.array),
      tree: normalizeCamera(cameraByView.tree)
    }
  };
}

export function loadPreferences() {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) {
      return createDefaultPreferences();
    }

    return normalizePreferences(JSON.parse(raw));
  } catch {
    return createDefaultPreferences();
  }
}

export function savePreferences(preferences) {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(preferences));
  } catch {
    // Ignore local storage failures.
  }
}

export { MIN_ZOOM, MAX_ZOOM };