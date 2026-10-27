mergeInto(LibraryManager.library, {
  KB_CanvasPixelRatio: function () {
    var canvas = Module.canvas;
    var width = canvas.getBoundingClientRect().width;
    return width > 0 ? canvas.width / width : 1;
  }
});
