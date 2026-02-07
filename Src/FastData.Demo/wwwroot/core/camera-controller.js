import { MAX_ZOOM, MIN_ZOOM } from "./preferences.js";

function clamp(value, min, max) {
  return Math.max(min, Math.min(max, value));
}

export class CameraController {
  constructor(options) {
    this.getActiveCamera = options.getActiveCamera;
    this.onCameraChanged = options.onCameraChanged;
    this.canvas = null;
    this.dragging = false;
    this.lastPointer = null;
  }

  attachCanvas(canvas) {
    this.canvas = canvas;

    this.canvas.addEventListener("wheel", event => this.onWheel(event), { passive: false });
    this.canvas.addEventListener("pointerdown", event => this.onPointerDown(event));
    this.canvas.addEventListener("pointermove", event => this.onPointerMove(event));
    this.canvas.addEventListener("pointerup", event => this.onPointerUp(event));
    this.canvas.addEventListener("pointercancel", event => this.onPointerUp(event));
    this.canvas.addEventListener("dblclick", event => {
      event.preventDefault();
      this.resetView();
    });
  }

  onWheel(event) {
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
    this.onCameraChanged();
  }

  onPointerDown(event) {
    if (!this.canvas || event.button !== 0) {
      return;
    }

    const pointer = this.getPointerPosition(event);
    if (!pointer) {
      return;
    }

    this.dragging = true;
    this.lastPointer = pointer;
    this.canvas.classList.add("is-panning");
    this.canvas.setPointerCapture(event.pointerId);
    event.preventDefault();
  }

  onPointerMove(event) {
    if (!this.canvas || !this.dragging) {
      return;
    }

    const pointer = this.getPointerPosition(event);
    if (!pointer || !this.lastPointer) {
      return;
    }

    const dx = pointer.x - this.lastPointer.x;
    const dy = pointer.y - this.lastPointer.y;
    this.lastPointer = pointer;
    this.panBy(dx, dy);
    event.preventDefault();
  }

  onPointerUp(event) {
    if (!this.canvas) {
      return;
    }

    this.dragging = false;
    this.lastPointer = null;
    this.canvas.classList.remove("is-panning");
    this.onCameraChanged();

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

  zoomAt(screenX, screenY, factor) {
    if (factor === 0) {
      return;
    }

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
    this.onCameraChanged();
  }
}