using System.Collections.Generic;
using UnityEngine;

namespace KitchenGoldfishMemory.Views
{
    /*
     * The coffee cup appears to now lack a sliced Sprite object anywhere in the game's assets.
     * It could only be found as unsliced pixel content inside the "Default Sprite"
     * Texture2D. There's nothing to reference by name; the art is created procedurally here.
     */
    static class GoldfishMemoryIcons
    {
        internal enum Icon
        {
            CoffeeCup,
            Pot,
        }

        const string TEXTURE_NAME = "Default Sprite";

        static readonly Dictionary<Icon, Rect> PIXEL_RECTS = new Dictionary<Icon, Rect>()
        {
            { Icon.CoffeeCup, new Rect(1033, 1045, 238, 213) },
            { Icon.Pot, new Rect(1033, 1296, 238, 225) },
        };

        static Texture2D _texture;
        static bool _searchedForTexture = false;
        static readonly Dictionary<Icon, Sprite> _sprites = new Dictionary<Icon, Sprite>();

        internal static Sprite Get(Icon icon)
        {
            if (_sprites.TryGetValue(icon, out Sprite existing) && existing != null)
                return existing;

            if (!_searchedForTexture)
            {
                _searchedForTexture = true;
                foreach (Texture2D candidate in Resources.FindObjectsOfTypeAll<Texture2D>())
                {
                    if (candidate.name == TEXTURE_NAME)
                    {
                        _texture = candidate;
                        break;
                    }
                }

                if (_texture == null)
                    Main.LogWarning($"Could not find a loaded texture named '{TEXTURE_NAME}' for procedural icons.");
            }

            if (_texture == null)
                return null;

            if (!PIXEL_RECTS.TryGetValue(icon, out Rect rect))
            {
                Main.LogWarning($"No pixel rect registered for icon '{icon}'.");
                return null;
            }

            try
            {
                Sprite sprite = Sprite.Create(_texture, rect, new Vector2(0.5f, 0.5f), 100f);
                sprite.name = $"GoldfishMemory_{icon}";
                _sprites[icon] = sprite;
                return sprite;
            }
            catch (System.Exception ex)
            {
                Main.LogError($"Sprite.Create for icon '{icon}' threw: {ex}");
                return null;
            }
        }
    }
}
