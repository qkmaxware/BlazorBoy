var GBRenderer = (function() {
    var colours = [
        // Background
        {r: 0, g: 0, b: 0},         // Dark
        {r: 89, g: 89, b: 89},      // Medium
        {r: 166, g: 166, b: 166},   // Light
        {r: 255, g: 255, b: 255},   // "White"
        // Object Pallet 0
        {r: 0, g: 0, b: 0},         // Dark
        {r: 89, g: 89, b: 89},      // Medium
        {r: 166, g: 166, b: 166},   // Light
        {r: 255, g: 255, b: 255},   // "White"
        // Object Pallet 1
        {r: 0, g: 0, b: 0},         // Dark
        {r: 89, g: 89, b: 89},      // Medium
        {r: 166, g: 166, b: 166},   // Light
        {r: 255, g: 255, b: 255},   // "White"
    ];
    
    const LCD_HEIGHT = 144;
    const LCD_WIDTH = 160;
    var LCD_PIXELS = new ImageData(LCD_WIDTH, LCD_HEIGHT);
    var LCD_PIXEL_INDICES = Array(LCD_WIDTH * LCD_HEIGHT);
    async function PushPixels(canvasRef, pixels) {
        if (!canvasRef)
            return;
        
        // https://stackoverflow.com/questions/20832299/how-to-draw-on-an-html5-canvas-pixel-by-pixel
        // Create "unscaled" image
        var img =  LCD_PIXELS;
        var len = img.data.length;
        var pixelCount = len / 4;
        var wasEdited = false;
        for (var i = 0; i < pixelCount; i++) {
            var colour = colours[0]; // default black;
            if (i < pixels.length) {
                var colourIndex = pixels[i];
                if (colourIndex >= 0 && colourIndex < colours.length) {
                    colour = colours[colourIndex];
                }
            }
            if (LCD_PIXEL_INDICES[i] === colour)
                continue; // Pixel didn't change, don't update the image data
    
            img.data[4*i + 0] = colour.r;
            img.data[4*i + 1] = colour.g;
            img.data[4*i + 2] = colour.b;
            img.data[4*i + 3] = 255;
            LCD_PIXEL_INDICES[i] = colour;
            wasEdited = true;
        }
        // Scale image to canvas and draw it
        // More performant to do this only if we actually changed something...
        if (wasEdited) {
            const ctx = canvasRef.getContext("2d");
            const bitmap = await createImageBitmap(img);
            ctx.imageSmoothingEnabled = false; // keep pixel perfect
            ctx.clearRect(0, 0, canvasRef.width, canvasRef.height);
            ctx.drawImage(bitmap, 0, 0, canvasRef.width, canvasRef.height);
        }
    }

    return {
        PushPixels
    }
})();