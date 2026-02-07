import { VisualizationEngine } from "./core/engine.js";
import { createLinearSearch } from "./algorithms/linear-search.js";
import { createBinarySearch } from "./algorithms/binary-search.js";
import { createInterpolationSearch } from "./algorithms/interpolation-search.js";
import { createSixteenArySearch } from "./algorithms/sixteen-ary-search.js";
import { createEytzingerSearch } from "./algorithms/eytzinger-search.js";

const engine = new VisualizationEngine({
  linear: createLinearSearch,
  binary: createBinarySearch,
  interpolation: createInterpolationSearch,
  sixteenAry: createSixteenArySearch,
  eytzinger: createEytzingerSearch
});

window.addEventListener("DOMContentLoaded", () => engine.attachUi(document));

new p5(p => {
  p.setup = () => {
    const canvas = p.createCanvas(p.windowWidth, p.windowHeight);
    canvas.parent("vizLayer");
    p.frameRate(15);
  };

  p.draw = () => {
    p.background("#0f2330");
    p.noStroke();

    engine.update(p.millis());
    engine.draw(p);
  };

  p.windowResized = () => p.resizeCanvas(p.windowWidth, p.windowHeight);
});
