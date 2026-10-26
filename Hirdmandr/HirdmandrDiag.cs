using UnityEngine;

namespace Hirdmandr
{
    // Self-terminating diagnostic attached to a freshly spawned hirdmandr NPC.
    // It logs, once per second, the local player's control/movement state and the NPC's
    // AI state, so we can see exactly what happens to the local player the moment the
    // NPC exists. It dies with the NPC (e.g. via /hirdmandr destroy), so it never lingers.
    internal class HirdmandrDiag : MonoBehaviour
    {
        // Captured at spawn time so we keep a valid handle to the local player even if
        // Player.m_localPlayer is later null'd or reassigned by something.
        public Player m_localPlayerAtSpawn;

        // The 1-second control snapshot was a temporary diagnostic for the spawn/ownership bug;
        // that is now confirmed working and the try/catch safety net floats any real error to the
        // log, so the snapshot is off by default. Flip back to true to re-arm the per-second dump.
        public bool m_log = false;

        private float m_nextLog;
        private int m_tickCount;

        void Update()
        {
            try
            {
            if (!m_log)
            {
                return;
            }
            if (m_tickCount >= 5)
            {
                return;
            }
            if (Time.time < m_nextLog)
            {
                return;
            }
            m_nextLog = Time.time + 1f;
            m_tickCount++;
            LogSnapshot();
            }
            catch (System.Exception e)
            {
                Jotunn.Logger.LogError("HirdmandrDiag.Update threw:\n" + e);
            }
        }

        private void LogSnapshot()
        {
            Player mlp = Player.m_localPlayer;
            bool mlpNull = (mlp == null);
            bool mlpIsLocal = (mlp == m_localPlayerAtSpawn);
            string mlpName = mlpNull ? "<null>" : mlp.m_name;

            string localName = "<no ref>";
            string moveDir = "<no ref>";
            string vel = "<no ref>";
            float velMag = -1f;
            bool kin = false;
            bool dead = false;
            bool lpEnabled = false;

            var lp = m_localPlayerAtSpawn;
            if (lp != null)
            {
                localName = lp.m_name;
                moveDir = lp.m_moveDir.ToString();
                var rb = lp.GetComponent<Rigidbody>();
                Vector3 v = (rb != null) ? rb.velocity : Vector3.zero;
                kin = (rb != null && rb.isKinematic);
                dead = lp.IsDead();
                lpEnabled = lp.enabled;
                vel = string.Format("({0:F1},{1:F1},{2:F1})", v.x, v.y, v.z);
                velMag = v.magnitude;
            }

            var mai = GetComponent<MonsterAI>();
            bool maiEnabled = (mai != null && mai.enabled);
            bool maiAlerted = (mai != null && mai.IsAlerted());
            GameObject follow = (mai != null) ? mai.GetFollowTarget() : null;
            string followName = (follow != null) ? follow.name : "<none>";
            float dist = (lp != null) ? Vector3.Distance(transform.position, lp.transform.position) : -1f;

            // Which Awake branch ran + why: owner flag and the ZDO name key that decides
            // between the fresh-NPC init (name=="") and the ZDOtoAppearance load (name!="").
            var view = GetComponent<ZNetView>();
            bool own = (view != null && view.IsOwner());
            var zdo = (view != null) ? view.GetZDO() : null;
            string zid = (zdo != null) ? zdo.m_uid.ToString() : "<nozdo>";
            string zname = (zdo != null) ? zdo.GetString("hmnpc_name") : "<nozdo>";

            Jotunn.Logger.LogInfo(string.Format(
                "[HirdmandrDiag] t={0:F1} mlpExists={1} mlpNull={2} mlpName={3} mlpIsLocal={4} | local={5} moveDir={6} vel={7} velMag={8:F2} kin={9} dead={10} en={11} | MAIen={12} MAIalert={13} follow={14} dist={15:F1} | own={16} zdo={17} name='{18}'",
                Time.time,
                Player.m_localPlayerExists,
                mlpNull,
                mlpName,
                mlpIsLocal,
                localName,
                moveDir,
                vel,
                velMag,
                kin,
                dead,
                lpEnabled,
                maiEnabled,
                maiAlerted,
                followName,
                dist,
                own,
                zid,
                zname));
        }
    }
}
