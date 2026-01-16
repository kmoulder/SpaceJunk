using Godot;

// SpaceFactory

/// <summary>
/// SpriteGenerator - Procedural pixel art generator (Autoload singleton).
/// Creates textures for items, buildings, and other game objects.
/// </summary>
public partial class SpriteGenerator : Node
{
    /// <summary>
    /// Singleton instance
    /// </summary>
    public static SpriteGenerator Instance { get; private set; }

    /// <summary>
    /// Cache for generated textures
    /// </summary>
    private readonly System.Collections.Generic.Dictionary<string, Texture2D> _textureCache = new();

    public override void _EnterTree()
    {
        GD.Print("[SpriteGenerator] _EnterTree called");
        Instance = this;
    }

    public override void _Ready()
    {
        GD.Print("[SpriteGenerator] _Ready called");
    }

    /// <summary>
    /// Generate ore/raw material sprite
    /// </summary>
    public Texture2D GenerateOre(Color color, int seed = 0)
    {
        string cacheKey = $"ore_{color}_{seed}";
        if (_textureCache.TryGetValue(cacheKey, out var cached))
            return cached;

        int size = 32;
        var img = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        img.Fill(Colors.Transparent);

        var rng = new RandomNumberGenerator();
        rng.Seed = (ulong)(color.ToArgb32() + seed);

        // Draw irregular chunky ore shape
        Color darkColor = color.Darkened(0.3f);
        Color lightColor = color.Lightened(0.2f);

        // Main ore body
        for (int i = 0; i < 5; i++)
        {
            int cx = rng.RandiRange(10, 22);
            int cy = rng.RandiRange(10, 22);
            int radius = rng.RandiRange(4, 8);

            DrawFilledCircle(img, cx, cy, radius, color);
        }

        // Add highlights
        for (int i = 0; i < 8; i++)
        {
            int x = rng.RandiRange(8, 24);
            int y = rng.RandiRange(8, 24);
            if (img.GetPixel(x, y).A > 0)
            {
                img.SetPixel(x, y, lightColor);
            }
        }

        // Add shadows
        for (int i = 0; i < 6; i++)
        {
            int x = rng.RandiRange(8, 24);
            int y = rng.RandiRange(8, 24);
            if (img.GetPixel(x, y).A > 0)
            {
                img.SetPixel(x, y, darkColor);
            }
        }

        var texture = ImageTexture.CreateFromImage(img);
        _textureCache[cacheKey] = texture;
        return texture;
    }

    /// <summary>
    /// Generate plate/processed material sprite
    /// </summary>
    public Texture2D GeneratePlate(Color color)
    {
        string cacheKey = $"plate_{color}";
        if (_textureCache.TryGetValue(cacheKey, out var cached))
            return cached;

        int size = 32;
        var img = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        img.Fill(Colors.Transparent);

        Color darkColor = color.Darkened(0.2f);
        Color lightColor = color.Lightened(0.3f);

        // Draw plate rectangle
        for (int x = 6; x < 26; x++)
        {
            for (int y = 10; y < 22; y++)
            {
                img.SetPixel(x, y, color);
            }
        }

        // Top highlight
        for (int x = 6; x < 26; x++)
        {
            img.SetPixel(x, 10, lightColor);
        }

        // Left highlight
        for (int y = 10; y < 22; y++)
        {
            img.SetPixel(6, y, lightColor);
        }

        // Bottom shadow
        for (int x = 6; x < 26; x++)
        {
            img.SetPixel(x, 21, darkColor);
        }

        // Right shadow
        for (int y = 10; y < 22; y++)
        {
            img.SetPixel(25, y, darkColor);
        }

        var texture = ImageTexture.CreateFromImage(img);
        _textureCache[cacheKey] = texture;
        return texture;
    }

    /// <summary>
    /// Generate gear sprite
    /// </summary>
    public Texture2D GenerateGear(Color color)
    {
        string cacheKey = $"gear_{color}";
        if (_textureCache.TryGetValue(cacheKey, out var cached))
            return cached;

        int size = 32;
        var img = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        img.Fill(Colors.Transparent);

        Color darkColor = color.Darkened(0.2f);

        int cx = 16, cy = 16;

        // Outer teeth
        for (int i = 0; i < 8; i++)
        {
            float angle = i * Mathf.Pi / 4.0f;
            int tx = cx + (int)(Mathf.Cos(angle) * 10);
            int ty = cy + (int)(Mathf.Sin(angle) * 10);
            DrawFilledCircle(img, tx, ty, 3, color);
        }

        // Main body
        DrawFilledCircle(img, cx, cy, 8, color);

        // Inner hole
        DrawFilledCircle(img, cx, cy, 3, darkColor);

        var texture = ImageTexture.CreateFromImage(img);
        _textureCache[cacheKey] = texture;
        return texture;
    }

    /// <summary>
    /// Generate cable sprite
    /// </summary>
    public Texture2D GenerateCable(Color color)
    {
        string cacheKey = $"cable_{color}";
        if (_textureCache.TryGetValue(cacheKey, out var cached))
            return cached;

        int size = 32;
        var img = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        img.Fill(Colors.Transparent);

        Color darkColor = color.Darkened(0.2f);

        // Draw coiled cable
        for (int i = 0; i < 3; i++)
        {
            int y = 10 + i * 4;
            for (int x = 8; x < 24; x++)
            {
                img.SetPixel(x, y, color);
                img.SetPixel(x, y + 1, color);
                img.SetPixel(x, y + 2, darkColor);
            }
        }

        var texture = ImageTexture.CreateFromImage(img);
        _textureCache[cacheKey] = texture;
        return texture;
    }

    /// <summary>
    /// Generate circuit sprite
    /// </summary>
    public Texture2D GenerateCircuit(Color color, int tier = 1)
    {
        string cacheKey = $"circuit_{color}_{tier}";
        if (_textureCache.TryGetValue(cacheKey, out var cached))
            return cached;

        int size = 32;
        var img = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        img.Fill(Colors.Transparent);

        Color boardColor = new(0.1f, 0.3f, 0.1f);
        Color traceColor = color.Lightened(0.3f);

        // Circuit board
        for (int x = 6; x < 26; x++)
        {
            for (int y = 8; y < 24; y++)
            {
                img.SetPixel(x, y, boardColor);
            }
        }

        // Traces
        for (int x = 8; x < 24; x += 4)
        {
            for (int y = 10; y < 22; y++)
            {
                img.SetPixel(x, y, traceColor);
            }
        }

        for (int y = 12; y < 20; y += 4)
        {
            for (int x = 8; x < 24; x++)
            {
                img.SetPixel(x, y, traceColor);
            }
        }

        // Chip in center
        for (int x = 12; x < 20; x++)
        {
            for (int y = 12; y < 20; y++)
            {
                img.SetPixel(x, y, color);
            }
        }

        var texture = ImageTexture.CreateFromImage(img);
        _textureCache[cacheKey] = texture;
        return texture;
    }

    /// <summary>
    /// Generate furnace sprite
    /// </summary>
    public Texture2D GenerateFurnace(bool isElectric)
    {
        string cacheKey = $"furnace_{isElectric}";
        if (_textureCache.TryGetValue(cacheKey, out var cached))
            return cached;

        int size = Constants.TileSize * 2;
        var img = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        img.Fill(Colors.Transparent);

        Color bodyColor = isElectric ? new Color(0.3f, 0.3f, 0.4f) : new Color(0.5f, 0.4f, 0.3f);
        Color darkColor = bodyColor.Darkened(0.2f);
        Color fireColor = new(1.0f, 0.5f, 0.1f);

        // Main body
        for (int x = 4; x < size - 4; x++)
        {
            for (int y = 8; y < size - 4; y++)
            {
                img.SetPixel(x, y, bodyColor);
            }
        }

        // Top opening
        for (int x = 16; x < size - 16; x++)
        {
            for (int y = 4; y < 12; y++)
            {
                img.SetPixel(x, y, darkColor);
            }
        }

        // Fire glow
        for (int x = 20; x < size - 20; x++)
        {
            for (int y = size / 2; y < size / 2 + 10; y++)
            {
                img.SetPixel(x, y, fireColor);
            }
        }

        // Border
        for (int x = 4; x < size - 4; x++)
        {
            img.SetPixel(x, 8, darkColor);
            img.SetPixel(x, size - 5, darkColor);
        }
        for (int y = 8; y < size - 4; y++)
        {
            img.SetPixel(4, y, darkColor);
            img.SetPixel(size - 5, y, darkColor);
        }

        var texture = ImageTexture.CreateFromImage(img);
        _textureCache[cacheKey] = texture;
        return texture;
    }

    /// <summary>
    /// Generate chest sprite
    /// </summary>
    public Texture2D GenerateChest(Color color)
    {
        string cacheKey = $"chest_{color}";
        if (_textureCache.TryGetValue(cacheKey, out var cached))
            return cached;

        int size = Constants.TileSize;
        var img = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        img.Fill(Colors.Transparent);

        Color darkColor = color.Darkened(0.3f);
        Color lightColor = color.Lightened(0.2f);

        // Main body
        for (int x = 4; x < size - 4; x++)
        {
            for (int y = 8; y < size - 4; y++)
            {
                img.SetPixel(x, y, color);
            }
        }

        // Lid
        for (int x = 4; x < size - 4; x++)
        {
            for (int y = 4; y < 10; y++)
            {
                img.SetPixel(x, y, lightColor);
            }
        }

        // Latch
        for (int x = 14; x < 18; x++)
        {
            for (int y = 14; y < 18; y++)
            {
                img.SetPixel(x, y, new Color(0.8f, 0.7f, 0.2f));
            }
        }

        // Border
        for (int x = 4; x < size - 4; x++)
        {
            img.SetPixel(x, 4, darkColor);
            img.SetPixel(x, size - 5, darkColor);
        }
        for (int y = 4; y < size - 4; y++)
        {
            img.SetPixel(4, y, darkColor);
            img.SetPixel(size - 5, y, darkColor);
        }

        var texture = ImageTexture.CreateFromImage(img);
        _textureCache[cacheKey] = texture;
        return texture;
    }

    /// <summary>
    /// Generate belt sprite
    /// </summary>
    public Texture2D GenerateBelt(Enums.Direction direction)
    {
        string cacheKey = $"belt_{direction}";
        if (_textureCache.TryGetValue(cacheKey, out var cached))
            return cached;

        int size = Constants.TileSize;
        var img = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        img.Fill(Colors.Transparent);

        Color beltColor = new(0.4f, 0.4f, 0.3f);
        Color stripeColor = new(0.6f, 0.6f, 0.2f);

        // Belt base
        for (int x = 4; x < size - 4; x++)
        {
            for (int y = 4; y < size - 4; y++)
            {
                img.SetPixel(x, y, beltColor);
            }
        }

        // Direction arrows/stripes
        var dirVec = Enums.DirectionToVector(direction);

        if (dirVec.X != 0)
        {
            // Horizontal belt
            for (int stripe = 0; stripe < 4; stripe++)
            {
                int x = 6 + stripe * 6;
                for (int y = 8; y < size - 8; y++)
                {
                    img.SetPixel(x, y, stripeColor);
                }
            }
        }
        else
        {
            // Vertical belt
            for (int stripe = 0; stripe < 4; stripe++)
            {
                int y = 6 + stripe * 6;
                for (int x = 8; x < size - 8; x++)
                {
                    img.SetPixel(x, y, stripeColor);
                }
            }
        }

        var texture = ImageTexture.CreateFromImage(img);
        _textureCache[cacheKey] = texture;
        return texture;
    }

    /// <summary>
    /// Generate inserter sprite
    /// </summary>
    public Texture2D GenerateInserter(bool isLong)
    {
        string cacheKey = $"inserter_{isLong}";
        if (_textureCache.TryGetValue(cacheKey, out var cached))
            return cached;

        int size = Constants.TileSize;
        var img = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        img.Fill(Colors.Transparent);

        Color baseColor = new(0.5f, 0.5f, 0.2f);
        Color armColor = new(0.6f, 0.6f, 0.3f);

        // Base platform
        for (int x = 8; x < 24; x++)
        {
            for (int y = 20; y < 28; y++)
            {
                img.SetPixel(x, y, baseColor);
            }
        }

        // Arm
        int armLength = isLong ? 20 : 14;
        for (int y = 16 - armLength; y < 20; y++)
        {
            for (int x = 14; x < 18; x++)
            {
                if (y >= 0)
                    img.SetPixel(x, y, armColor);
            }
        }

        var texture = ImageTexture.CreateFromImage(img);
        _textureCache[cacheKey] = texture;
        return texture;
    }

    /// <summary>
    /// Generate solar panel sprite
    /// </summary>
    public Texture2D GenerateSolarPanel()
    {
        string cacheKey = "solar_panel";
        if (_textureCache.TryGetValue(cacheKey, out var cached))
            return cached;

        int size = Constants.TileSize * 2;
        var img = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        img.Fill(Colors.Transparent);

        Color frameColor = new(0.3f, 0.3f, 0.35f);
        Color panelColor = new(0.1f, 0.1f, 0.3f);
        Color cellColor = new(0.2f, 0.2f, 0.5f);

        // Frame
        for (int x = 2; x < size - 2; x++)
        {
            for (int y = 2; y < size - 2; y++)
            {
                img.SetPixel(x, y, frameColor);
            }
        }

        // Panel interior
        for (int x = 6; x < size - 6; x++)
        {
            for (int y = 6; y < size - 6; y++)
            {
                img.SetPixel(x, y, panelColor);
            }
        }

        // Solar cells
        for (int cellX = 0; cellX < 4; cellX++)
        {
            for (int cellY = 0; cellY < 4; cellY++)
            {
                int startX = 8 + cellX * 12;
                int startY = 8 + cellY * 12;
                for (int x = startX; x < startX + 10; x++)
                {
                    for (int y = startY; y < startY + 10; y++)
                    {
                        if (x < size && y < size)
                            img.SetPixel(x, y, cellColor);
                    }
                }
            }
        }

        var texture = ImageTexture.CreateFromImage(img);
        _textureCache[cacheKey] = texture;
        return texture;
    }

    /// <summary>
    /// Generate assembler sprite
    /// </summary>
    public Texture2D GenerateAssembler(int tier = 1)
    {
        string cacheKey = $"assembler_{tier}";
        if (_textureCache.TryGetValue(cacheKey, out var cached))
            return cached;

        int size = Constants.TileSize * 2;
        var img = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        img.Fill(Colors.Transparent);

        // Tier-based colors
        Color bodyColor = tier switch
        {
            1 => new Color(0.35f, 0.35f, 0.4f),
            2 => new Color(0.3f, 0.4f, 0.5f),
            _ => new Color(0.4f, 0.35f, 0.45f)
        };
        Color darkColor = bodyColor.Darkened(0.25f);
        Color accentColor = tier switch
        {
            1 => new Color(0.6f, 0.5f, 0.2f),
            2 => new Color(0.2f, 0.6f, 0.4f),
            _ => new Color(0.5f, 0.3f, 0.6f)
        };

        // Main body
        for (int x = 4; x < size - 4; x++)
        {
            for (int y = 4; y < size - 4; y++)
            {
                img.SetPixel(x, y, bodyColor);
            }
        }

        // Border
        for (int x = 4; x < size - 4; x++)
        {
            img.SetPixel(x, 4, darkColor);
            img.SetPixel(x, size - 5, darkColor);
        }
        for (int y = 4; y < size - 4; y++)
        {
            img.SetPixel(4, y, darkColor);
            img.SetPixel(size - 5, y, darkColor);
        }

        // Mechanical arm / gear representation in center
        int cx = size / 2;
        int cy = size / 2;
        DrawFilledCircle(img, cx, cy, 12, darkColor);
        DrawFilledCircle(img, cx, cy, 8, accentColor);
        DrawFilledCircle(img, cx, cy, 4, darkColor);

        // Input/output indicators
        // Left side (input)
        for (int y = 20; y < 44; y++)
        {
            img.SetPixel(6, y, new Color(0.3f, 0.5f, 0.3f));
            img.SetPixel(7, y, new Color(0.3f, 0.5f, 0.3f));
        }

        // Right side (output)
        for (int y = 20; y < 44; y++)
        {
            img.SetPixel(size - 7, y, new Color(0.5f, 0.4f, 0.2f));
            img.SetPixel(size - 8, y, new Color(0.5f, 0.4f, 0.2f));
        }

        var texture = ImageTexture.CreateFromImage(img);
        _textureCache[cacheKey] = texture;
        return texture;
    }

    /// <summary>
    /// Generate lab sprite
    /// </summary>
    public Texture2D GenerateLab()
    {
        string cacheKey = "lab";
        if (_textureCache.TryGetValue(cacheKey, out var cached))
            return cached;

        int size = Constants.TileSize * 2;
        var img = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        img.Fill(Colors.Transparent);

        Color bodyColor = new(0.3f, 0.35f, 0.4f);
        Color darkColor = bodyColor.Darkened(0.2f);
        Color glassColor = new(0.4f, 0.6f, 0.7f);
        Color flaskColor = new(0.8f, 0.2f, 0.2f);
        Color liquidColor = new(0.2f, 0.8f, 0.3f);

        // Main body
        for (int x = 4; x < size - 4; x++)
        {
            for (int y = 8; y < size - 4; y++)
            {
                img.SetPixel(x, y, bodyColor);
            }
        }

        // Border
        for (int x = 4; x < size - 4; x++)
        {
            img.SetPixel(x, 8, darkColor);
            img.SetPixel(x, size - 5, darkColor);
        }
        for (int y = 8; y < size - 4; y++)
        {
            img.SetPixel(4, y, darkColor);
            img.SetPixel(size - 5, y, darkColor);
        }

        // Glass dome/window on top
        for (int x = 16; x < size - 16; x++)
        {
            for (int y = 4; y < 16; y++)
            {
                img.SetPixel(x, y, glassColor);
            }
        }

        // Flask/beaker on left side
        for (int x = 10; x < 22; x++)
        {
            for (int y = 24; y < 44; y++)
            {
                img.SetPixel(x, y, flaskColor);
            }
        }
        // Flask liquid
        for (int x = 12; x < 20; x++)
        {
            for (int y = 32; y < 42; y++)
            {
                img.SetPixel(x, y, liquidColor);
            }
        }

        // Flask/beaker on right side (different color)
        Color flask2Color = new(0.2f, 0.4f, 0.8f);
        Color liquid2Color = new(0.8f, 0.6f, 0.2f);
        for (int x = size - 22; x < size - 10; x++)
        {
            for (int y = 24; y < 44; y++)
            {
                img.SetPixel(x, y, flask2Color);
            }
        }
        // Flask liquid
        for (int x = size - 20; x < size - 12; x++)
        {
            for (int y = 34; y < 42; y++)
            {
                img.SetPixel(x, y, liquid2Color);
            }
        }

        // Science symbol in center (stylized atom)
        Color atomColor = new(0.9f, 0.9f, 0.2f);
        int cx = size / 2;
        int cy = 36;
        DrawFilledCircle(img, cx, cy, 4, atomColor);

        var labTexture = ImageTexture.CreateFromImage(img);
        _textureCache[cacheKey] = labTexture;
        return labTexture;
    }

    /// <summary>
    /// Generate foundation tile sprite
    /// </summary>
    public Texture2D GenerateFoundation()
    {
        string cacheKey = "foundation";
        if (_textureCache.TryGetValue(cacheKey, out var cached))
            return cached;

        int size = Constants.TileSize;
        var img = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);

        Color baseColor = new(0.2f, 0.2f, 0.25f);
        Color gridColor = new(0.15f, 0.15f, 0.2f);

        img.Fill(baseColor);

        // Grid lines
        for (int x = 0; x < size; x++)
        {
            img.SetPixel(x, 0, gridColor);
            img.SetPixel(x, size - 1, gridColor);
        }
        for (int y = 0; y < size; y++)
        {
            img.SetPixel(0, y, gridColor);
            img.SetPixel(size - 1, y, gridColor);
        }

        var texture = ImageTexture.CreateFromImage(img);
        _textureCache[cacheKey] = texture;
        return texture;
    }

    /// <summary>
    /// Generate debris sprite
    /// </summary>
    public Texture2D GenerateDebris(string debrisType, int seed = 0)
    {
        string cacheKey = $"debris_{debrisType}_{seed}";
        if (_textureCache.TryGetValue(cacheKey, out var cached))
            return cached;

        Color baseColor = debrisType switch
        {
            "iron_asteroid" => new Color(0.5f, 0.4f, 0.35f),
            "copper_asteroid" => new Color(0.7f, 0.5f, 0.3f),
            "stone_asteroid" => new Color(0.5f, 0.5f, 0.5f),
            "coal_asteroid" => new Color(0.2f, 0.2f, 0.2f),
            "scrap_metal" => new Color(0.4f, 0.4f, 0.45f),
            "ice_chunk" => new Color(0.7f, 0.8f, 0.9f),
            _ => new Color(0.5f, 0.5f, 0.5f)
        };

        var texture = GenerateOre(baseColor, seed);
        _textureCache[cacheKey] = texture;
        return texture;
    }

    /// <summary>
    /// Generate generic building sprite
    /// </summary>
    public Texture2D GenerateBuilding(Color color, Vector2I sizeInTiles)
    {
        string cacheKey = $"building_{color}_{sizeInTiles.X}x{sizeInTiles.Y}";
        if (_textureCache.TryGetValue(cacheKey, out var cached))
            return cached;

        int width = sizeInTiles.X * Constants.TileSize;
        int height = sizeInTiles.Y * Constants.TileSize;
        var img = Image.CreateEmpty(width, height, false, Image.Format.Rgba8);
        img.Fill(Colors.Transparent);

        Color darkColor = color.Darkened(0.2f);

        // Main body
        for (int x = 2; x < width - 2; x++)
        {
            for (int y = 2; y < height - 2; y++)
            {
                img.SetPixel(x, y, color);
            }
        }

        // Border
        for (int x = 2; x < width - 2; x++)
        {
            img.SetPixel(x, 2, darkColor);
            img.SetPixel(x, height - 3, darkColor);
        }
        for (int y = 2; y < height - 2; y++)
        {
            img.SetPixel(2, y, darkColor);
            img.SetPixel(width - 3, y, darkColor);
        }

        var texture = ImageTexture.CreateFromImage(img);
        _textureCache[cacheKey] = texture;
        return texture;
    }

    /// <summary>
    /// Helper: Draw filled circle on image
    /// </summary>
    private static void DrawFilledCircle(Image img, int cx, int cy, int radius, Color color)
    {
        for (int x = cx - radius; x <= cx + radius; x++)
        {
            for (int y = cy - radius; y <= cy + radius; y++)
            {
                if (x >= 0 && x < img.GetWidth() && y >= 0 && y < img.GetHeight())
                {
                    float dist = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    if (dist <= radius)
                    {
                        img.SetPixel(x, y, color);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Clear the texture cache
    /// </summary>
    public void ClearCache()
    {
        _textureCache.Clear();
    }
}
