using HarmonyLib;
using Kitchen;
using KitchenData;
using KitchenGoldfishMemory.Views;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace KitchenGoldfishMemory.Patches
{
    [HarmonyPatch]
    static class CustomerIndicatorView_Patch
    {
        const string ICON_OVERLAY_CHILD_NAME = "GoldfishMemory_IconOverlay";

        // Pull overlay toward the camera to overtake vanilla behaviour
        const float CAMERA_OFFSET = 0.5f;

        static Dictionary<MenuPhase, string> _menuPhaseSprites = new Dictionary<MenuPhase, string>()
        {
            { MenuPhase.Starter, "food_card" },
            { MenuPhase.Main, "service" },
        };
        static Dictionary<MenuPhase, string> MenuPhaseSprites => _menuPhaseSprites;

        // Menu phase handled via a procedurally-created Sprite overlay instead of a TMP
        // sprite-tag string -- see GoldfishMemoryIcons.cs. Game version 1.5 or thereabouts
        // broke the coffee sprite previously used.
        static readonly Dictionary<MenuPhase, GoldfishMemoryIcons.Icon> _menuPhaseOverlayIcons = new Dictionary<MenuPhase, GoldfishMemoryIcons.Icon>()
        {
            { MenuPhase.Dessert, GoldfishMemoryIcons.Icon.CoffeeCup },
        };

        static bool _loggedNoGroupView = false;

        [HarmonyPatch(typeof(CustomerIndicatorView), "UpdateData")]
        [HarmonyPostfix]
        static void UpdateData_Postfix(CustomerIndicatorView.ViewData view_data, ref TextMeshPro ___Icon, ref CustomerIndicatorView __instance)
        {
            if (!view_data.HasPatience ||
                ___Icon == null ||
                __instance.GetComponent<GroupMenuPhaseView>() == null)
            {
                if (___Icon != null && __instance.GetComponent<GroupMenuPhaseView>() == null && !_loggedNoGroupView)
                {
                    _loggedNoGroupView = true;
                    // Main.LogWarning("CustomerIndicatorView.UpdateData postfix reached, but GetComponent<GroupMenuPhaseView>() " +
                    //                  "returned null — LocalViewRouter_Patch's GetPrefab postfix never added it to this instance.");
                }
                return;
            }

            GroupMenuPhaseView groupMealPhaseView = __instance.GetComponent<GroupMenuPhaseView>();
            if (groupMealPhaseView == null)
                return;

            GoldfishMemoryIcons.Icon overlayIcon = default;
            bool useOverlay = view_data.PatienceReason == PatienceReason.Service &&
                               _menuPhaseOverlayIcons.TryGetValue(groupMealPhaseView.MenuPhase, out overlayIcon);

            SetIconOverlayVisible(__instance, ___Icon.transform, useOverlay, useOverlay ? overlayIcon : default);

            if (useOverlay)
            {
                // The overlay sprite handles the icon entirely for this case; blank the TMP
                // text so it doesn't draw a stale/fallback glyph behind or alongside it.
                ___Icon.text = groupMealPhaseView.IsRepeatPhase ? "<size=80%><color=#ffffff><sup>2" : string.Empty;
                return;
            }

            string icon;
            switch (view_data.PatienceReason)
            {
                case PatienceReason.Service:
                    TryGetIcon(groupMealPhaseView.MenuPhase, out icon);
                    break;
                default:
                    icon = GameData.Main.GlobalLocalisation.GetIcon(view_data.PatienceReason);
                    break;
            }

            if (groupMealPhaseView.IsRepeatPhase)
                ___Icon.text = $"<size=80%>{icon}<color=#ffffff><sup>2";
            else
                ___Icon.text = icon;
        }

        static Material _referenceSpriteMaterial;
        static bool _searchedForMaterial = false;

        static Material FindWorkingSpriteMaterial()
        {
            if (_searchedForMaterial)
                return _referenceSpriteMaterial;
            _searchedForMaterial = true;

            foreach (SpriteRenderer candidate in Resources.FindObjectsOfTypeAll<SpriteRenderer>())
            {
                if (candidate.sharedMaterial != null && candidate.sprite != null)
                {
                    _referenceSpriteMaterial = candidate.sharedMaterial;
                    break;
                }
            }

            if (_referenceSpriteMaterial == null)
                Main.LogWarning("Could not find any existing SpriteRenderer with a material to copy for the icon overlay -- it may not render.");

            return _referenceSpriteMaterial;
        }
        
        static readonly Dictionary<CustomerIndicatorView, GameObject> _overlaysByInstance = new Dictionary<CustomerIndicatorView, GameObject>();
        static readonly List<CustomerIndicatorView> _pruneScratch = new List<CustomerIndicatorView>();

        static void PruneDestroyedOverlays()
        {
            _pruneScratch.Clear();
            foreach (KeyValuePair<CustomerIndicatorView, GameObject> kvp in _overlaysByInstance)
            {
                if (kvp.Key == null) // Unity's overloaded null check: true for destroyed objects too
                {
                    if (kvp.Value != null)
                        UnityEngine.Object.Destroy(kvp.Value);
                    _pruneScratch.Add(kvp.Key);
                }
            }
            foreach (CustomerIndicatorView dead in _pruneScratch)
                _overlaysByInstance.Remove(dead);
        }

        static void SetIconOverlayVisible(CustomerIndicatorView instance, Transform iconTransform, bool visible, GoldfishMemoryIcons.Icon icon)
        {
            PruneDestroyedOverlays();

            _overlaysByInstance.TryGetValue(instance, out GameObject overlay);

            if (!visible)
            {
                if (overlay != null)
                    overlay.SetActive(false);
                return;
            }

            SpriteRenderer renderer;
            if (overlay == null)
            {
                overlay = new GameObject(ICON_OVERLAY_CHILD_NAME);
                overlay.layer = iconTransform.gameObject.layer;
                overlay.transform.localScale = Vector3.one * 0.11f;
                renderer = overlay.AddComponent<SpriteRenderer>();

                Material workingMaterial = FindWorkingSpriteMaterial();
                if (workingMaterial != null)
                    renderer.sharedMaterial = workingMaterial;

                Renderer iconRenderer = iconTransform.GetComponent<Renderer>();
                if (iconRenderer != null)
                {
                    renderer.sortingLayerID = iconRenderer.sortingLayerID;
                    renderer.sortingOrder = iconRenderer.sortingOrder + 1;
                }

                _overlaysByInstance[instance] = overlay;
            }
            else
            {
                renderer = overlay.GetComponent<SpriteRenderer>();
            }

            // Re-synced every call (not just at creation) so the overlay tracks the customer
            // as they move, same as the TMP icon it's standing in for. Pulled toward the
            // camera by CAMERA_OFFSET to win the depth test against occluding geometry.
            // Uses the ray from the camera TO THIS OBJECT, not Camera.main.transform.forward
            // (the camera's general view direction) -- for a perspective camera those only
            // coincide for objects dead-center on screen; using camera-forward visibly
            // shifted the icon sideways off the customer during testing.
            Vector3 towardCamera = Vector3.zero;
            if (Camera.main != null)
            {
                Vector3 toObject = (iconTransform.position - Camera.main.transform.position).normalized;
                towardCamera = -toObject * CAMERA_OFFSET;
            }
            overlay.transform.position = iconTransform.position + towardCamera;
            overlay.transform.rotation = iconTransform.rotation;

            Sprite sprite = GoldfishMemoryIcons.Get(icon);
            if (renderer.sprite != sprite)
                renderer.sprite = sprite;

            overlay.SetActive(sprite != null);
        }

        static bool TryGetIcon(MenuPhase menuPhase, out string icon)
        {
            if (MenuPhaseSprites.TryGetValue(menuPhase, out string iconName))
            {
                icon = $"<sprite name=\"{iconName}\">";
                return true;
            }
            icon = default;
            return false;
        }
    }
}
