using System.Security.Cryptography.X509Certificates;
using Qkmaxware.Vm.LR35902;

namespace Qkmaxware.Emulators.Gameboy.Hardware;

public partial class Ppu : IPpu {

    private void flushBuffer() {
        var temp = this.buffer;
        this.buffer = Canvas;
        this.Canvas = temp;
        HasBufferJustFlushed = true;
    }

    struct ScanrowPixel {
        public byte ColourId;
        public ColourPallet DecodedColour;
    }
    ScanrowPixel[] scanrow = new ScanrowPixel[LCD_WIDTH];
    private void renderScanline() {
        if(!LCDC.IsLcdEnabled){ 
            return;
        }

        if(LCDC.IsBackgroundEnabled) {
            renderBackgroundLine(this.ScanLineIndex);
        }

        if (LCDC.IsWindowEnabled) {
            renderWindowLine(this.ScanLineIndex);
        }
        
        if(LCDC.IsSpritesEnabled){
            renderSpriteLine(this.ScanLineIndex);
        }

        shiftPixels(this.ScanLineIndex);
    }

    // The 8000 method uses $8000 as a base pointer and adds (TILE_NUMBER * 16) to it
    private TileSpan GetTile8000Method(int index) => this.tiles[index];
    // The 8800 method uses $9000 as a base pointer and adds (SIGNED_TILE_NUMBER * 16)
    private TileSpan GetTile8800Method(int index) => this.tiles[256 + index]; // (36864 - 32768) / 16 = 256

    protected int unsignedByteToSigned(int u8){
        if(u8 > 127)
            u8 = -((~u8+1)&255);
        return u8;
    }

    private void renderBackgroundLine(int scanline) {
        int background_y = Viewport.Y;
        int background_x = Viewport.X;
        var background = this.BackgroundMap;

        var bgPixelY = (background_y + scanline) % 256;  // Handle screen wrapping
        var tileY = bgPixelY / TileSpan.Height;
        for (var x = 0; x < LCD_WIDTH; x++) {
            var bgPixelX = (background_x + x) % 256; // Handle screen wrapping
            // Determine the tile to access
            var tileX = bgPixelX / TileSpan.Width;
            var tileIndex = (int)background.TileIndexAt(tileX, tileY);
            var tile
                = LCDC.TileDataSelect == TileDataSelect.Method8800
                ? GetTile8800Method(unsignedByteToSigned(tileIndex)) 
                : GetTile8000Method(tileIndex); 
            // Determine the pixel in the tile to access
            int pixelX = bgPixelX - tileX * TileSpan.Width;
            int pixelY = bgPixelY - tileY * TileSpan.Height;
            // Add it to the buffer
            var colourIndex = tile[pixelX, pixelY];         // Get the pixel from the tile
            var colour = BackgroundPalette[colourIndex];    // Get the colour of that pixel from the palette
            scanrow[x].ColourId = colourIndex;
            scanrow[x].DecodedColour = colour;
        }
    }

    private void renderWindowLine(int scanline) {
        /*
        Window is between x = 0 and 166 and y = 0 and 143. 
        Interestingly, the X coordinate is x – 7 as 7 would be the left side of the LCD. 
        These coordinate imply that the window is drawn relative to the LCD, not the background.
        */
        var windowX = this.Window.X - 7;
        var windowY = this.Window.Y;
        var background = this.WindowMap;

        // If the scanline intersects the window
        if (windowY <= scanline) {
            // Compute the tile that falls on this scanline
            var bgPixelY = (scanline - windowY) % 256;
            var tileY = bgPixelY / TileSpan.Height;

            // Check each pixel on the scanline
            for (var screenX = 0; screenX < LCD_WIDTH; screenX++) {
                // Skip pixel if the window is offset to the right
                if (screenX < windowX)
                    continue;

                // Convert the coordinate to pixels on the background
                var bgPixelX = (screenX - windowX) % 256;
                var tileX = bgPixelX / TileSpan.Width;
                var tileIndex = (int)background.TileIndexAt(tileX, tileY);
                var tile
                    = LCDC.TileDataSelect == TileDataSelect.Method8800
                    ? GetTile8800Method(unsignedByteToSigned(tileIndex)) 
                    : GetTile8000Method(tileIndex); 

                // Determine the pixel in the tile to access
                int pixelX = bgPixelX - tileX * TileSpan.Width;
                int pixelY = bgPixelY - tileY * TileSpan.Height;

                // Add it to the buffer
                var colourIndex = tile[pixelX, pixelY];         // Get the pixel from the tile
                var colour = BackgroundPalette[colourIndex];    // Get the colour of that pixel from the palette
                scanrow[screenX].ColourId = colourIndex;
                scanrow[screenX].DecodedColour = colour;
            }
        }
    }

    private void renderSpriteLine(int scanline) {
        var screenY = scanline;
        int spritesize = LCDC.SpriteSize == SpriteSize.Long1x2 ? 16 : 8;

        foreach (var sprite in this.sprites_sorted) {
            // Does the sprite land on the scanline
            if (!(sprite.YTop <= scanline))
                continue;
            if (!(sprite.YTop + spritesize > scanline))
                continue;

            // Get the tiles
            var topTile    = LCDC.SpriteSize == SpriteSize.Long1x2 ? this.GetTile8000Method(sprite.TileNumber & 0xFE) : this.GetTile8000Method(sprite.TileNumber);   
            var bottomTile = LCDC.SpriteSize == SpriteSize.Long1x2 ? this.GetTile8000Method(sprite.TileNumber | 0x01) : null;         
            if (sprite.FlipY == SpriteSpan.YOrientation.Flipped) {
                var temp = topTile;                 // If y flipped, swap the top and bottom sprites
                topTile = bottomTile ?? topTile;
                bottomTile = topTile;
            }

            int pixelY = scanline - sprite.YTop; // scanline should be > YTop so this is positive
            var sampledTile = pixelY < 8 ? topTile : (bottomTile ?? topTile);
            pixelY = pixelY < 8 ? pixelY : (pixelY - 8);

            // If y flipped, grab the opposite horizontal row
            if (sprite.FlipY == SpriteSpan.YOrientation.Flipped) {
                pixelY = (TileSpan.Height - 1) - pixelY; 
            }

            // Get the colour palette
            var palette = sprite.PaletteNumber switch {
                SpriteSpan.PaletteIndex.Zero => this.Object0Palette,
                SpriteSpan.PaletteIndex.One => this.Object1Palette,
                _ => this.Object0Palette
            };

            // Draw the sprite
            for (var scanX = 0; scanX < TileSpan.Width; scanX++) {
                // Pixel must be on the screen
                int screenX = sprite.XLeft + scanX;

                // Pixel must be on the screen to be drawn
                if(screenX < 0 || screenX >= LCD_WIDTH)
                    continue;

                // Flip x coordinate if required
                var pixelX = scanX;
                if (sprite.FlipX == SpriteSpan.XOrientation.Flipped) {
                    pixelX = (TileSpan.Width - 1) - scanX;
                }

                // Get and mix pixel
                var pixel = sampledTile[pixelX, pixelY];
                var colour = palette[pixel];
                mixIn(screenX, pixel, colour, sprite);
            }
        }
    }

    private void mixIn(int x, byte spriteColourIndex, ColourPallet pixelFromSprite, SpriteSpan sprite) {
        // If the color number of the Sprite Pixel is 0, the Background Pixel is pushed to the LCD.
        if (spriteColourIndex == 0) {
            return;
        }
        // If the BG-to-OBJ-Priority bit is 1 and the color number of the Background Pixel is anything other than 0, the Background Pixel is pushed to the LCD.
        if (sprite.Priority == SpriteSpan.DrawPriority.BelowBackground && scanrow[x].ColourId != 0) {
            return;
        }
        // If none of the above conditions apply, the Sprite Pixel is pushed to the LCD.
        scanrow[x].ColourId = spriteColourIndex;
        scanrow[x].DecodedColour = pixelFromSprite;
    }

    private void shiftPixels(int scanline) {
        for (var i = 0; i < LCD_WIDTH; i++) {
            this.buffer[i, scanline] = scanrow[i].DecodedColour;
        }
    }
}