using RimWorld;
using Verse;
using Verse.Sound;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace InspirationTab
{
    [StaticConstructorOnStartup]
    public static class InspirationTabLoader
    {
        static InspirationTabLoader()
        {
            Log.Message("[InspirationTab] Mod loaded successfully.");
        }
    }

    public class MainTabWindow_Inspirations : MainTabWindow
    {
        private Vector2 colonistScrollPosition = Vector2.zero;
        private Vector2 inspirationScrollPosition = Vector2.zero;
        private Pawn selectedColonist = null;
        private InspirationDef selectedInspiration = null;
        private List<InspirationDef> allInspirations = null;
        private string searchText = "";

        public override Vector2 RequestedTabSize
        {
            get { return new Vector2(900f, 600f); }
        }

        public MainTabWindow_Inspirations()
        {
            this.doCloseButton = true;
            this.doCloseX = true;
            this.forcePause = false;
        }

        public override void PreOpen()
        {
            base.PreOpen();
            // Cache all inspiration defs
            List<InspirationDef> tempList = DefDatabase<InspirationDef>.AllDefsListForReading.ToList();
            tempList.Sort((a, b) => GetInspirationLabel(a).CompareTo(GetInspirationLabel(b)));
            allInspirations = tempList;
            selectedColonist = null;
            selectedInspiration = null;
            searchText = "";
        }

        private string GetInspirationLabel(InspirationDef def)
        {
            if (def.label != null) return def.label;
            return def.defName;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, inRect.width, 35f), "Grant Inspirations to Colonists");
            Text.Font = GameFont.Small;

            float topMargin = 40f;
            float columnWidth = (inRect.width - 20f) / 3f;
            float listHeight = inRect.height - topMargin - 80f;

            // Column 1: Colonist List
            Rect colonistRect = new Rect(0f, topMargin, columnWidth, listHeight);
            DrawColonistList(colonistRect);

            // Column 2: Inspiration List
            Rect inspirationRect = new Rect(columnWidth + 10f, topMargin, columnWidth, listHeight);
            DrawInspirationList(inspirationRect);

            // Column 3: Info and Grant Button
            Rect actionRect = new Rect((columnWidth + 10f) * 2f, topMargin, columnWidth, listHeight);
            DrawActionPanel(actionRect);
        }

        private void DrawColonistList(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            
            Rect headerRect = new Rect(rect.x + 5f, rect.y + 5f, rect.width - 10f, 25f);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(headerRect, "Select Colonist");
            Text.Anchor = TextAnchor.UpperLeft;

            List<Pawn> colonists = new List<Pawn>();
            if (Find.CurrentMap != null && Find.CurrentMap.mapPawns != null)
            {
                IEnumerable<Pawn> freeColonists = Find.CurrentMap.mapPawns.FreeColonistsSpawned;
                if (freeColonists != null)
                {
                    colonists = freeColonists.ToList();
                }
            }
            
            Rect scrollViewRect = new Rect(rect.x + 5f, rect.y + 35f, rect.width - 10f, rect.height - 40f);
            Rect scrollContentRect = new Rect(0f, 0f, scrollViewRect.width - 20f, colonists.Count * 35f);

            Widgets.BeginScrollView(scrollViewRect, ref colonistScrollPosition, scrollContentRect);
            
            float y = 0f;
            foreach (Pawn colonist in colonists)
            {
                Rect rowRect = new Rect(0f, y, scrollContentRect.width, 32f);
                
                // Highlight selected
                if (selectedColonist == colonist)
                {
                    Widgets.DrawHighlightSelected(rowRect);
                }
                else if (Mouse.IsOver(rowRect))
                {
                    Widgets.DrawHighlight(rowRect);
                }

                // Draw pawn icon
                Rect iconRect = new Rect(rowRect.x + 2f, rowRect.y + 2f, 28f, 28f);
                Widgets.ThingIcon(iconRect, colonist);

                // Draw name
                Rect nameRect = new Rect(iconRect.xMax + 5f, rowRect.y, rowRect.width - 35f, 32f);
                Text.Anchor = TextAnchor.MiddleLeft;
                string colonistLabel = GetColonistLabel(colonist);
                
                // Show current inspiration if any
                if (colonist.Inspired)
                {
                    colonistLabel += " (" + colonist.InspirationDef.label + ")";
                }
                
                Widgets.Label(nameRect, colonistLabel);
                Text.Anchor = TextAnchor.UpperLeft;

                if (Widgets.ButtonInvisible(rowRect))
                {
                    selectedColonist = colonist;
                    SoundDefOf.Click.PlayOneShotOnCamera(null);
                }

                y += 35f;
            }

            Widgets.EndScrollView();
        }

        private string GetColonistLabel(Pawn colonist)
        {
            if (colonist.Name != null)
            {
                return colonist.Name.ToStringShort;
            }
            return colonist.LabelCap;
        }

        private void DrawInspirationList(Rect rect)
        {
            Widgets.DrawMenuSection(rect);

            Rect headerRect = new Rect(rect.x + 5f, rect.y + 5f, rect.width - 10f, 25f);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(headerRect, "Select Inspiration");
            Text.Anchor = TextAnchor.UpperLeft;

            // Search box
            Rect searchRect = new Rect(rect.x + 5f, rect.y + 32f, rect.width - 10f, 24f);
            searchText = Widgets.TextField(searchRect, searchText);

            // Filter inspirations
            List<InspirationDef> filteredInspirations = allInspirations;
            if (!searchText.NullOrEmpty())
            {
                string searchLower = searchText.ToLower();
                filteredInspirations = new List<InspirationDef>();
                foreach (InspirationDef insp in allInspirations)
                {
                    string label = insp.label != null ? insp.label.ToLower() : "";
                    string defName = insp.defName.ToLower();
                    if (label.Contains(searchLower) || defName.Contains(searchLower))
                    {
                        filteredInspirations.Add(insp);
                    }
                }
            }

            Rect scrollViewRect = new Rect(rect.x + 5f, rect.y + 60f, rect.width - 10f, rect.height - 65f);
            Rect scrollContentRect = new Rect(0f, 0f, scrollViewRect.width - 20f, filteredInspirations.Count * 30f);

            Widgets.BeginScrollView(scrollViewRect, ref inspirationScrollPosition, scrollContentRect);

            float y = 0f;
            foreach (InspirationDef insp in filteredInspirations)
            {
                Rect rowRect = new Rect(0f, y, scrollContentRect.width, 28f);

                if (selectedInspiration == insp)
                {
                    Widgets.DrawHighlightSelected(rowRect);
                }
                else if (Mouse.IsOver(rowRect))
                {
                    Widgets.DrawHighlight(rowRect);
                }

                Text.Anchor = TextAnchor.MiddleLeft;
                string label = insp.label != null ? insp.label.CapitalizeFirst() : insp.defName;
                Widgets.Label(new Rect(rowRect.x + 5f, rowRect.y, rowRect.width - 5f, rowRect.height), label);
                Text.Anchor = TextAnchor.UpperLeft;

                if (Widgets.ButtonInvisible(rowRect))
                {
                    selectedInspiration = insp;
                    SoundDefOf.Click.PlayOneShotOnCamera(null);
                }

                y += 30f;
            }

            Widgets.EndScrollView();
        }

        private void DrawActionPanel(Rect rect)
        {
            Widgets.DrawMenuSection(rect);

            Rect headerRect = new Rect(rect.x + 5f, rect.y + 5f, rect.width - 10f, 25f);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(headerRect, "Action");
            Text.Anchor = TextAnchor.UpperLeft;

            float infoY = rect.y + 40f;
            float infoX = rect.x + 10f;
            float infoWidth = rect.width - 20f;

            // Show selected colonist info
            Text.Font = GameFont.Small;
            string colonistName = "None selected";
            if (selectedColonist != null && selectedColonist.Name != null)
            {
                colonistName = selectedColonist.Name.ToStringFull;
            }
            else if (selectedColonist != null)
            {
                colonistName = selectedColonist.LabelCap;
            }
            Widgets.Label(new Rect(infoX, infoY, infoWidth, 24f), "Colonist: " + colonistName);
            infoY += 28f;

            if (selectedColonist != null && selectedColonist.Inspired)
            {
                GUI.color = Color.yellow;
                Widgets.Label(new Rect(infoX, infoY, infoWidth, 24f), "Current: " + selectedColonist.InspirationDef.label);
                GUI.color = Color.white;
                infoY += 28f;
            }

            infoY += 10f;

            // Show selected inspiration info
            string inspirationName = "None selected";
            if (selectedInspiration != null)
            {
                inspirationName = selectedInspiration.label != null ? selectedInspiration.label.CapitalizeFirst() : selectedInspiration.defName;
            }
            Widgets.Label(new Rect(infoX, infoY, infoWidth, 24f), "Inspiration: " + inspirationName);
            infoY += 28f;

            // Show inspiration description
            if (selectedInspiration != null && !selectedInspiration.description.NullOrEmpty())
            {
                Rect descRect = new Rect(infoX, infoY, infoWidth, 100f);
                GUI.color = new Color(0.8f, 0.8f, 0.8f);
                Widgets.Label(descRect, selectedInspiration.description);
                GUI.color = Color.white;
                infoY += 110f;
            }

            infoY += 20f;

            // Grant button
            bool canGrant = selectedColonist != null && selectedInspiration != null;
            
            Rect grantButtonRect = new Rect(rect.x + 20f, infoY, rect.width - 40f, 35f);
            
            if (!canGrant)
            {
                GUI.color = Color.gray;
            }

            if (Widgets.ButtonText(grantButtonRect, "Grant Inspiration", true, true, canGrant) && canGrant)
            {
                GrantInspiration(selectedColonist, selectedInspiration);
            }
            
            GUI.color = Color.white;
            infoY += 45f;

            // Remove current inspiration button
            if (selectedColonist != null && selectedColonist.Inspired)
            {
                Rect removeButtonRect = new Rect(rect.x + 20f, infoY, rect.width - 40f, 35f);
                GUI.color = new Color(1f, 0.5f, 0.5f);
                if (Widgets.ButtonText(removeButtonRect, "Remove Current Inspiration"))
                {
                    RemoveInspiration(selectedColonist);
                }
                GUI.color = Color.white;
            }
        }

        private void GrantInspiration(Pawn pawn, InspirationDef inspiration)
        {
            if (pawn == null || inspiration == null) return;

            // Remove any existing inspiration first
            if (pawn.Inspired)
            {
                pawn.mindState.inspirationHandler.EndInspiration(pawn.InspirationDef);
            }

            // Try to start the new inspiration (force it)
            bool success = pawn.mindState.inspirationHandler.TryStartInspiration(inspiration, null, true);
            
            if (success)
            {
                Messages.Message("Granted " + inspiration.label + " to " + pawn.Name.ToStringShort + "!", MessageTypeDefOf.PositiveEvent, false);
                SoundDefOf.Click.PlayOneShotOnCamera(null);
            }
            else
            {
                // Force it by trying a different approach
                try
                {
                    // If TryStartInspiration failed (because of checks), we can try to directly create and assign
                    Inspiration newInspiration = (Inspiration)System.Activator.CreateInstance(inspiration.inspirationClass);
                    newInspiration.def = inspiration;
                    newInspiration.pawn = pawn;
                    newInspiration.PostStart(false);
                    
                    // Use reflection to set the current inspiration
                    var handler = pawn.mindState.inspirationHandler;
                    var curField = typeof(InspirationHandler).GetField("curState", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (curField != null)
                    {
                        curField.SetValue(handler, newInspiration);
                    }
                    else
                    {
                        // Try alternative field name
                        curField = typeof(InspirationHandler).GetField("curInspiration", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (curField != null)
                        {
                            curField.SetValue(handler, newInspiration);
                        }
                    }
                    
                    Messages.Message("Force granted " + inspiration.label + " to " + pawn.Name.ToStringShort + "!", MessageTypeDefOf.PositiveEvent, false);
                }
                catch (System.Exception ex)
                {
                    Log.Warning("[InspirationTab] Could not force grant inspiration: " + ex.Message);
                    Messages.Message("Could not grant inspiration to " + pawn.Name.ToStringShort + ". They may not be eligible.", MessageTypeDefOf.RejectInput, false);
                }
            }
        }

        private void RemoveInspiration(Pawn pawn)
        {
            if (pawn == null || !pawn.Inspired) return;

            string inspirationName = pawn.InspirationDef.label;
            pawn.mindState.inspirationHandler.EndInspiration(pawn.InspirationDef);
            
            Messages.Message("Removed " + inspirationName + " from " + pawn.Name.ToStringShort + ".", MessageTypeDefOf.NeutralEvent, false);
            SoundDefOf.Click.PlayOneShotOnCamera(null);
        }
    }
}
