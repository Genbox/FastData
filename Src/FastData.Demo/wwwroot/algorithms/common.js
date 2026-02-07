function clamp(value, min, max) {
  return Math.max(min, Math.min(max, value));
}

function randomInt(rng, min, max) {
  return Math.floor(rng() * (max - min + 1)) + min;
}

function shuffleInPlace(rng, values) {
  for (let i = values.length - 1; i > 0; i -= 1) {
    const j = randomInt(rng, 0, i);
    const tmp = values[i];
    values[i] = values[j];
    values[j] = tmp;
  }
}

function uniqueSampleFromPool(rng, size, min, max) {
  const pool = [];

  for (let value = min; value <= max; value += 1) {
    pool.push(value);
  }

  shuffleInPlace(rng, pool);
  return pool.slice(0, size);
}

function buildUniformValues(size, start, step) {
  const values = new Array(size);

  for (let i = 0; i < size; i += 1) {
    values[i] = start + i * step;
  }

  return values;
}

export function createRng(seed) {
  let state = seed >>> 0 || 1;

  return () => {
    state = state * 1664525 + 1013904223 >>> 0;
    return state / 4294967296;
  };
}

export function buildRandomArray(size, datasetMode = "random", seed = 42, min = 10, max = 99) {
  const rng = createRng(seed);
  const values = [];

  if (datasetMode === "uniform") {
    const start = randomInt(rng, min, min + size);
    const step = randomInt(rng, 1, 3);
    return buildUniformValues(size, start, step);
  }

  if (datasetMode === "clustered") {
    const centers = [
      randomInt(rng, min + 8, max - 8),
      randomInt(rng, min + 8, max - 8),
      randomInt(rng, min + 8, max - 8)
    ];

    for (let i = 0; i < size; i += 1) {
      const center = centers[randomInt(rng, 0, centers.length - 1)];
      values.push(clamp(center + randomInt(rng, -7, 7), min, max));
    }

    return values;
  }

  if (datasetMode === "nearlySorted") {
    const sorted = [];

    for (let i = 0; i < size; i += 1) {
      sorted.push(clamp(min + i, min, max));
    }

    const swaps = Math.max(1, Math.floor(size * 0.08));
    for (let i = 0; i < swaps; i += 1) {
      const a = randomInt(rng, 0, size - 1);
      const b = randomInt(rng, 0, size - 1);
      const tmp = sorted[a];
      sorted[a] = sorted[b];
      sorted[b] = tmp;
    }

    return sorted;
  }

  for (let i = 0; i < size; i += 1) {
    values.push(randomInt(rng, min, max));
  }

  return values;
}

export function buildSortedUniqueArray(size, datasetMode = "random", seed = 42, min = 8, max = 99) {
  const rng = createRng(seed);
  const adjustedMax = Math.max(max, min + size * 4);

  if (datasetMode === "uniform") {
    const step = randomInt(rng, 1, 3);
    const start = randomInt(rng, min, min + size);
    return buildUniformValues(size, start, step).sort((a, b) => a - b);
  }

  if (datasetMode === "clustered") {
    const pool = new Set();
    const centers = [
      randomInt(rng, min + size, min + size * 2),
      randomInt(rng, min + size * 2, min + size * 3),
      randomInt(rng, min + size * 3, min + size * 4)
    ];
    const radius = Math.max(8, Math.ceil(size / 3) + 2);

    for (const center of centers) {
      for (let offset = -radius; offset <= radius; offset += 1) {
        pool.add(clamp(center + offset, min, adjustedMax));
      }
    }

    if (pool.size < size) {
      for (let value = min; value <= adjustedMax && pool.size < size; value += 1) {
        pool.add(value);
      }
    }

    const values = [...pool];
    shuffleInPlace(rng, values);
    return values.slice(0, size).sort((a, b) => a - b);
  }

  if (datasetMode === "nearlySorted") {
    const values = [];
    let current = randomInt(rng, min, min + 5);

    for (let i = 0; i < size; i += 1) {
      values.push(current);
      current += randomInt(rng, 1, 3);
    }

    return values;
  }

  return uniqueSampleFromPool(rng, size, min, adjustedMax).sort((a, b) => a - b);
}

export function toEytzinger(sorted) {
  const eytzinger = new Array(sorted.length);
  let cursor = 0;

  function fill(index) {
    if (index >= sorted.length) {
      return;
    }

    fill(index * 2 + 1);
    eytzinger[index] = sorted[cursor];
    cursor += 1;
    fill(index * 2 + 2);
  }

  fill(0);
  return eytzinger;
}