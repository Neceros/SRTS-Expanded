using UnityEngine;
using Verse;

namespace SRTS
{
    [StaticConstructorOnStartup]
    public static class Tex2D
    {
        public static readonly Texture2D LauncherTargeting = ContentFinder<Texture2D>.Get("UI/Overlays/LaunchableMouseAttachment", true);

        public static readonly Texture2D LaunchSRTS = ContentFinder<Texture2D>.Get("UI/Commands/LaunchShip", true);

        public static readonly Texture2D FuelSRTS = ContentFinder<Texture2D>.Get("Things/Item/Resource/Chemfuel", true);
    }
}