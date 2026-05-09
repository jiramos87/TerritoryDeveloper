using System;
using Territory.UI.Registry;
using UnityEngine;

namespace Territory.UI.Registry
{
    /// <summary>
    /// Wave A0 (TECH-27062) — seeds all 32 MainMenu-scope action ids + 12 bind ids into
    /// <see cref="UiActionRegistry"/> / <see cref="UiBindRegistry"/> at scene start.
    /// Stub handlers throw <see cref="NotImplementedException"/> — real handlers wired in Wave A1+.
    /// Mount alongside UiActionRegistry + UiBindRegistry on UI host GameObject.
    /// </summary>
    public class MainMenuRegistrySeed : MonoBehaviour
    {
        private void Awake()
        {
            var actions = GetComponent<UiActionRegistry>();
            var binds = GetComponent<UiBindRegistry>();

            if (actions == null)
            {
                Debug.LogError("[MainMenuRegistrySeed] UiActionRegistry not found on same GameObject.");
                return;
            }
            if (binds == null)
            {
                Debug.LogError("[MainMenuRegistrySeed] UiBindRegistry not found on same GameObject.");
                return;
            }

            RegisterActions(actions);
            RegisterBinds(binds);
        }

        private static void RegisterActions(UiActionRegistry r)
        {
            // ── main-menu (7) ─────────────────────────────────────────────
            r.Register("mainmenu.continue",         _ => throw new NotImplementedException("mainmenu.continue"));
            r.Register("mainmenu.openNewGame",       _ => throw new NotImplementedException("mainmenu.openNewGame"));
            r.Register("mainmenu.openLoad",          _ => throw new NotImplementedException("mainmenu.openLoad"));
            r.Register("mainmenu.openSettings",      _ => throw new NotImplementedException("mainmenu.openSettings"));
            r.Register("mainmenu.back",              _ => throw new NotImplementedException("mainmenu.back"));
            r.Register("mainmenu.quit.confirm",      _ => throw new NotImplementedException("mainmenu.quit.confirm"));
            r.Register("mainmenu.quit",              _ => throw new NotImplementedException("mainmenu.quit"));

            // ── new-game-form (4) ──────────────────────────────────────────
            r.Register("newgame.mapSize.set",        _ => throw new NotImplementedException("newgame.mapSize.set"));
            r.Register("newgame.budget.set",         _ => throw new NotImplementedException("newgame.budget.set"));
            r.Register("newgame.cityName.reroll",    _ => throw new NotImplementedException("newgame.cityName.reroll"));
            r.Register("mainmenu.startNewGame",      _ => throw new NotImplementedException("mainmenu.startNewGame"));

            // ── settings-view (12) ─────────────────────────────────────────
            r.Register("settings.scrollEdgePan.set",       _ => throw new NotImplementedException("settings.scrollEdgePan.set"));
            r.Register("settings.monthlyBudgetNotif.set",  _ => throw new NotImplementedException("settings.monthlyBudgetNotif.set"));
            r.Register("settings.autoSave.set",            _ => throw new NotImplementedException("settings.autoSave.set"));
            r.Register("settings.master.set",              _ => throw new NotImplementedException("settings.master.set"));
            r.Register("settings.music.set",               _ => throw new NotImplementedException("settings.music.set"));
            r.Register("settings.sfx.set",                 _ => throw new NotImplementedException("settings.sfx.set"));
            r.Register("settings.resolution.set",          _ => throw new NotImplementedException("settings.resolution.set"));
            r.Register("settings.fullscreen.set",          _ => throw new NotImplementedException("settings.fullscreen.set"));
            r.Register("settings.vsync.set",               _ => throw new NotImplementedException("settings.vsync.set"));
            r.Register("settings.resetDefaults.confirm",   _ => throw new NotImplementedException("settings.resetDefaults.confirm"));
            r.Register("settings.resetDefaults",           _ => throw new NotImplementedException("settings.resetDefaults"));
            r.Register("settings.back",                    _ => throw new NotImplementedException("settings.back"));

            // ── save-load-view (9) ─────────────────────────────────────────
            r.Register("saveload.save.confirm",      _ => throw new NotImplementedException("saveload.save.confirm"));
            r.Register("saveload.save",              _ => throw new NotImplementedException("saveload.save"));
            r.Register("saveload.overwrite.confirm", _ => throw new NotImplementedException("saveload.overwrite.confirm"));
            r.Register("saveload.overwrite",         _ => throw new NotImplementedException("saveload.overwrite"));
            r.Register("saveload.selectSlot",        _ => throw new NotImplementedException("saveload.selectSlot"));
            r.Register("saveload.load",              _ => throw new NotImplementedException("saveload.load"));
            r.Register("saveload.delete.confirm",    _ => throw new NotImplementedException("saveload.delete.confirm"));
            r.Register("saveload.delete",            _ => throw new NotImplementedException("saveload.delete"));
            r.Register("saveload.back",              _ => throw new NotImplementedException("saveload.back"));
        }

        private static void RegisterBinds(UiBindRegistry r)
        {
            // ── mainmenu binds (6) ─────────────────────────────────────────
            r.Set<object>("mainmenu.contentScreen",      null);   // enum
            r.Set<bool>  ("mainmenu.continue.disabled",  false);  // bool
            r.Set<bool>  ("mainmenu.back.visible",       false);  // bool
            r.Set<string>("mainmenu.title.text",         string.Empty);
            r.Set<string>("mainmenu.version.text",       string.Empty);
            r.Set<string>("mainmenu.studio.text",        string.Empty);

            // ── newgame binds (3) ──────────────────────────────────────────
            r.Set<object>("newgame.mapSize",             null);   // enum
            r.Set<object>("newgame.budget",              null);   // enum
            r.Set<string>("newgame.cityName",            string.Empty);

            // ── saveload binds (3) ─────────────────────────────────────────
            r.Set<object>("saveload.mode",               null);   // enum
            r.Set<object>("saveload.list",               null);   // array
            r.Set<string>("saveload.selectedSlot",       null);   // string|null
        }
    }
}
