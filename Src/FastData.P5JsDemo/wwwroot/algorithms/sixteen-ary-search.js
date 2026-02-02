import { buildSortedUniqueArray } from "./common.js";

function baseModel(data, target) {
  return {
    data,
    target,
    visited: new Array(data.length).fill(false),
    checkIndex: null,
    foundIndex: null,
    low: 0,
    high: data.length - 1,
    mid: null,
    pivotIndices: [],
    mode: "partition",
    linearCursor: null,
    comparisons: 0,
    done: false,
    outcome: "idle",
    status: "Split interval into 16 segments using 15 pivots.",
    comparisonText: "",
    activeLine: 0
  };
}

function uniqueSorted(values) {
  return [...new Set(values)].sort((a, b) => a - b);
}

function computePivots(low, high) {
  const span = high - low + 1;
  const pivots = [];

  for (let k = 1; k < 16; k += 1) {
    const raw = low + Math.floor((k * span) / 16);
    const pivot = Math.min(high - 1, Math.max(low + 1, raw));
    pivots.push(pivot);
  }

  return uniqueSorted(pivots);
}

export function createSixteenArySearch() {
  return {
    id: "sixteenAry",
    title: "16-ary Search",
    description: "Uses 15 pivots to split a sorted interval into 16 segments each round.",
    complexity: "~O(log16 n) rounds, with up to 15 pivot checks per round",
    pseudocode: [
      "while low <= high",
      "  choose 15 pivots that split [low, high] into 16 segments",
      "  compare target against pivots to select one segment",
      "  if pivot equals target: return pivot",
      "  continue inside chosen segment",
      "return -1"
    ],
    createModel(options) {
      const data = buildSortedUniqueArray(options.size);
      return baseModel(data, options.target);
    },
    resetModel(model) {
      return baseModel(model.data.slice(), model.target);
    },
    step(model) {
      if (model.done) {
        return;
      }

      if (model.low > model.high) {
        model.done = true;
        model.outcome = "not_found";
        model.status = "Target not found. Interval is empty.";
        model.comparisonText = "low > high";
        model.checkIndex = null;
        model.pivotIndices = [];
        model.activeLine = 5;
        return;
      }

      const span = model.high - model.low + 1;

      if (span <= 16) {
        if (model.mode !== "linear") {
          model.mode = "linear";
          model.linearCursor = model.low;
          model.pivotIndices = [];
          model.status = "Small interval detected. Finish with linear scan.";
          model.comparisonText = `Scanning indices ${model.low}..${model.high}`;
          model.activeLine = 4;
          return;
        }

        if (model.linearCursor > model.high) {
          model.done = true;
          model.outcome = "not_found";
          model.status = "Target not found in final linear scan.";
          model.comparisonText = "All remaining indices checked.";
          model.checkIndex = null;
          model.activeLine = 5;
          return;
        }

        const i = model.linearCursor;
        model.checkIndex = i;
        model.visited[i] = true;
        model.comparisons += 1;
        model.comparisonText = `Linear tail: arr[${i}] = ${model.data[i]} vs ${model.target}`;
        model.activeLine = 4;

        if (model.data[i] === model.target) {
          model.done = true;
          model.outcome = "found";
          model.foundIndex = i;
          model.status = `Found at index ${i}.`;
          model.activeLine = 3;
          return;
        }

        model.linearCursor += 1;
        model.status = `No match at ${i}. Continue linear tail scan.`;
        return;
      }

      model.mode = "partition";
      const pivots = computePivots(model.low, model.high);
      model.pivotIndices = pivots;
      model.activeLine = 2;

      let chosenSegment = pivots.length;
      let lastCheckedPivot = pivots[pivots.length - 1];

      for (let i = 0; i < pivots.length; i += 1) {
        const pivotIndex = pivots[i];
        const pivotValue = model.data[pivotIndex];
        model.visited[pivotIndex] = true;
        model.comparisons += 1;
        lastCheckedPivot = pivotIndex;

        if (model.target === pivotValue) {
          model.checkIndex = pivotIndex;
          model.foundIndex = pivotIndex;
          model.done = true;
          model.outcome = "found";
          model.status = `Matched pivot at index ${pivotIndex}.`;
          model.comparisonText = `arr[${pivotIndex}] = ${pivotValue} equals target ${model.target}`;
          model.activeLine = 3;
          return;
        }

        if (model.target < pivotValue) {
          chosenSegment = i;
          break;
        }
      }

      const nextLow = chosenSegment === 0 ? model.low : pivots[chosenSegment - 1] + 1;
      const nextHigh = chosenSegment === pivots.length ? model.high : pivots[chosenSegment] - 1;

      model.checkIndex = lastCheckedPivot;
      model.comparisonText = `Choose segment ${chosenSegment} => indices ${nextLow}..${nextHigh}`;

      if (nextLow > nextHigh) {
        model.done = true;
        model.outcome = "not_found";
        model.status = "No values remain after segment selection.";
        model.activeLine = 5;
        return;
      }

      model.low = nextLow;
      model.high = nextHigh;
      model.status = `Narrow search to ${model.low}..${model.high}.`;
      model.activeLine = 4;
    }
  };
}