import { buildSortedUniqueArray } from "./common.js";

function baseModel(data, target) {
  return {
    data,
    target,
    checkIndex: null,
    foundIndex: null,
    low: 0,
    high: data.length - 1,
    mid: null,
    comparisons: 0,
    done: false,
    outcome: "idle",
    status: "Estimate a probe position from the value distribution.",
    comparisonText: "",
    activeLine: 0
  };
}

export function createInterpolationSearch() {
  return {
    id: "interpolation",
    title: "Interpolation Search",
    description: "On sorted data, estimates where the target should be and probes there first.",
    complexity: "Average: O(log log n), worst-case: O(n)",
    pseudocode: [
      "while low <= high and target within [arr[low], arr[high]]",
      "  probe = low + ((target-arr[low])*(high-low))/(arr[high]-arr[low])",
      "  if arr[probe] == target: return probe",
      "  if arr[probe] < target: low = probe + 1",
      "  else: high = probe - 1",
      "return -1"
    ],
    createModel(options) {
      const data = buildSortedUniqueArray(options.size, options.datasetMode, options.seed);
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
        model.checkIndex = null;
        model.mid = null;
        model.status = "Target not found. Search interval is empty.";
        model.comparisonText = "low > high";
        model.activeLine = 5;
        return;
      }

      if (model.target < model.data[model.low] || model.target > model.data[model.high]) {
        model.done = true;
        model.outcome = "not_found";
        model.checkIndex = null;
        model.mid = null;
        model.status = "Target is outside the current value range.";
        model.comparisonText = `range is [${model.data[model.low]}, ${model.data[model.high]}]`;
        model.activeLine = 5;
        return;
      }

      const lowValue = model.data[model.low];
      const highValue = model.data[model.high];

      if (lowValue === highValue) {
        model.comparisons += 1;
        model.checkIndex = model.low;
        model.mid = model.low;
        model.comparisonText = `All values in range are ${lowValue}.`;

        if (lowValue === model.target) {
          model.done = true;
          model.outcome = "found";
          model.foundIndex = model.low;
          model.status = `Found at index ${model.low}.`;
          model.activeLine = 2;
          return;
        }

        model.done = true;
        model.outcome = "not_found";
        model.status = "Target not found in uniform range.";
        model.activeLine = 5;
        return;
      }

      model.activeLine = 1;
      const probe = model.low + Math.floor(
        ((model.target - lowValue) * (model.high - model.low)) / (highValue - lowValue)
      );

      model.mid = probe;
      model.checkIndex = probe;
      model.comparisons += 1;

      const value = model.data[probe];
      model.comparisonText = `Probe index ${probe}: arr[${probe}] = ${value}, target = ${model.target}`;

      if (value === model.target) {
        model.done = true;
        model.outcome = "found";
        model.foundIndex = probe;
        model.status = `Found at index ${probe}.`;
        model.activeLine = 2;
        return;
      }

      if (value < model.target) {
        model.low = probe + 1;
        model.status = `Target is larger than ${value}. Move low to ${model.low}.`;
        model.activeLine = 3;
        return;
      }

      model.high = probe - 1;
      model.status = `Target is smaller than ${value}. Move high to ${model.high}.`;
      model.activeLine = 4;
    }
  };
}