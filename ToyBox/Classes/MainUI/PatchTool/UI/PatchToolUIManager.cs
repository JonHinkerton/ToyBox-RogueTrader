﻿using ModKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ToyBox.PatchTool; 
public static class PatchToolUIManager {
    private static List<PatchToolTabUI> instances = new();
    private static int selectedIndex = -1;
    private static bool showExistingPatchesUI = false;
    public static void OnGUI() {
        Space(-25);
        Label("Warning:".localize().Yellow().Bold() + " " + "This is a very powerful feature. You won't break your game by simply changing the damage of a weapon,".localize().Yellow() + " " + "but this feature allows doing a lot of things that could potentially causes issues.".localize().Orange().Bold() + " " + "Beware of that and always keep a backup.".localize().Yellow());
        Label("Note:".localize().Green().Bold() + " " + "After finishing creating a patch, it is advised to restart the game before playing on a proper save.".localize().Green());
        Label("Warning:".localize().Yellow().Bold() + " " + "This feature is new. Please reach out if anything seems buggy or you encounter any issues.".localize().Yellow());
        Space(15);
        Div();
        Space(15);
        DisclosureToggle("Manage existing patches".localize(), ref showExistingPatchesUI, 200);
        if (showExistingPatchesUI) {
            PatchListUI.OnGUI();
        }
        Space(15);
        Div();
        Space(15);
        using (HorizontalScope()) {
            Label("Tabs".localize().Bold(), AutoWidth());
            Space(50);
            ActionButton("Create New Tab".localize(), () => {
                instances.Add(new PatchToolTabUI());
                selectedIndex = instances.Count - 1;
            }, AutoWidth());
        }
        Label("");
        using (VerticalScope()) {
            for (int j = 0; j < instances.Count; j += 4) {
                using (HorizontalScope()) {
                    for (int i = j; (i < instances.Count) && (i < j + 4); i++) {
                        string tabName = "New Tab".localize();
                        bool hasNoName = true;
                        if (!instances[i].Target.IsNullOrEmpty()) {
                            tabName = instances[i].Target;
                            hasNoName = false;
                        }
                        if (i == selectedIndex) {
                            if (hasNoName) {
                                Label(tabName, Width(300));
                            } else {
                                ClipboardLabel(tabName, Width(300));
                            }
                        } else {
                            ActionButton(tabName, () => {
                                selectedIndex = i;
                            }, Width(300));
                        }
                        ActionButton("Close".localize(), () => {
                            instances.RemoveAt(i);
                            if (selectedIndex >= instances.Count) {
                                selectedIndex = instances.Count - 1;
                            }
                        }, Width(70));
                        Space(50);
                    }
                }
                Label("");
            }
        }
        Space(15);
        Div();
        Space(15);
        if (selectedIndex >= 0 && selectedIndex < instances.Count) {
            instances[selectedIndex].OnGUI();
        } else {
            Label("No tabs open.".localize());
        }
    }
    public static void OpenBlueprintInTab(string guid) {
        var existing = instances.FirstOrDefault(tab => tab.Target.Equals(guid, StringComparison.InvariantCultureIgnoreCase));
        if (existing != default) {
            selectedIndex = instances.IndexOf(existing);
        } else {
            instances.Add(new PatchToolTabUI(guid));
            selectedIndex = instances.Count - 1;
        }
    }
}
