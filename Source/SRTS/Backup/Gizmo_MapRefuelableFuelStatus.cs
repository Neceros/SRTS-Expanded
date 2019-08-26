// Decompiled with JetBrains decompiler
// Type: Helicopter.Gizmo_MapRefuelableFuelStatus
// Assembly: Helicopter, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 87D55B68-45DE-4FF0-8B2F-4616963FFBF1
// Assembly location: D:\SteamLibrary\steamapps\workshop\content\294100\1833992261\Assemblies\Helicopter.dll

using System;
using UnityEngine;
using Verse;

namespace Helicopter
{
  [StaticConstructorOnStartup]
  public class Gizmo_MapRefuelableFuelStatus : Gizmo
  {
    private static readonly Texture2D FullBarTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.35f, 0.35f, 0.2f));
    private static readonly Texture2D EmptyBarTex = SolidColorMaterials.NewSolidColorTexture(Color.black);
    private static readonly Texture2D TargetLevelArrow = ContentFinder<Texture2D>.Get("UI/Misc/BarInstantMarkerRotated", true);
    public string compLabel;
    public float nowFuel;
    public float maxFuel;
    private const float ArrowScale = 0.5f;

    public override float GetWidth(float maxWidth)
    {
      return 140f;
    }

    public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth)
    {
      Rect overRect = new Rect(topLeft.x, topLeft.y, this.GetWidth(maxWidth), 75f);
      Find.WindowStack.ImmediateWindow(1523289473, overRect, WindowLayer.GameUI, (Action) (() =>
      {
        Rect rect1 = overRect.AtZero().ContractedBy(6f);
        Rect rect2 = rect1;
        rect2.height = overRect.height / 2f;
        Text.Font = GameFont.Tiny;
        Widgets.Label(rect2, this.compLabel);
        Rect rect3 = rect1;
        rect3.yMin = overRect.height / 2f;
        float fillPercent = this.nowFuel / this.maxFuel;
        Widgets.FillableBar(rect3, fillPercent, Gizmo_MapRefuelableFuelStatus.FullBarTex, Gizmo_MapRefuelableFuelStatus.EmptyBarTex, false);
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(rect3, this.nowFuel.ToString("F0") + " / " + this.maxFuel.ToString("F0"));
        Text.Anchor = TextAnchor.UpperLeft;
      }), true, false, 1f);
      return new GizmoResult(GizmoState.Clear);
    }
  }
}
