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
  const values = new Set();

  while (values.size < size) {
    values.add(randomInt(min, max));
  }

  return [...values].sort((a, b) => a - b);
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