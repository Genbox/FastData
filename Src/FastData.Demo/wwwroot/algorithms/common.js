function randomInt(min, max) {
  return Math.floor(Math.random() * (max - min + 1)) + min;
}

export function buildRandomArray(size, min = 10, max = 99) {
  const values = [];

  for (let i = 0; i < size; i += 1) {
    values.push(randomInt(min, max));
  }

  return values;
}

export function buildSortedUniqueArray(size, min = 8, max = 99) {
  const adjustedMax = Math.max(max, min + size * 2);
  const pool = [];

  for (let value = min; value <= adjustedMax; value += 1) {
    pool.push(value);
  }

  for (let i = pool.length - 1; i > 0; i -= 1) {
    const j = randomInt(0, i);
    const tmp = pool[i];
    pool[i] = pool[j];
    pool[j] = tmp;
  }

  return pool.slice(0, size).sort((a, b) => a - b);
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