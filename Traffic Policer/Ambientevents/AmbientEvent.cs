using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rage;
using LSPD_First_Response.Mod.API;

namespace Traffic_Policer.Ambientevents
{
    abstract internal class AmbientEvent
    {
        public Ped driver;
        protected bool eventRunning = true;
        public Vehicle car;
        protected float speed;
        protected Blip driverBlip;
        protected bool performingPullover = false;

        public GameFiber DrivingStyleFiber;
        public GameFiber AmbientEventMainFiber;
        public bool ReadyForGameFiberCleanup = false;
        public AmbientEvent() { }

        public AmbientEvent(bool ShowMessage, string Message)
        {
            if (ShowMessage)
            {
                Game.DisplayNotification(Message);
            }
        }

        public AmbientEvent(Ped Driver, bool CreateBlip, bool ShowMessage, string Message)
        {
            driver = Driver;
            driver.BlockPermanentEvents = true;
            driver.IsPersistent = true;
            car = driver.CurrentVehicle;
            car.IsPersistent = true;
            if (CreateBlip)
            {
                driverBlip = driver.AttachBlip();
                driverBlip.Color = System.Drawing.Color.Beige;
                driverBlip.Scale = 0.7f;
            }
            if (ShowMessage)
            {
                Game.DisplayNotification(Message);
            }           
        }


        protected abstract void MainLogic();
        protected virtual void End()
        {
            //Add gamefibers to garbage and clean
            eventRunning = false;
            if (driverBlip.Exists()) { driverBlip.Delete(); }
            if (!Functions.IsPlayerPerformingPullover() && !performingPullover)
            {
                if (driver.Exists() && (Functions.GetActivePursuit() == null || !Functions.GetPursuitPeds(Functions.GetActivePursuit()).Contains(driver)))
                {
                    driver.Dismiss();
                }

                if (car.Exists() && (!driver.Exists() || Functions.GetActivePursuit() == null || !Functions.GetPursuitPeds(Functions.GetActivePursuit()).Contains(driver)))
                {
                    car.Dismiss();
                }

            }
            else
            {
                if (TrafficPolicerHandler.IsLSPDFRPlusRunning)
                {
                    API.LSPDFRPlusFunctions.AddCountToStatistic(Main.PluginName, "Traffic ambient event vehicles pulled over");
                }
            }
            TrafficPolicerHandler.AmbientEventGameFibersToAbort.Add(DrivingStyleFiber);
            TrafficPolicerHandler.AmbientEventGameFibersToAbort.Add(AmbientEventMainFiber);
            Game.LogTrivial("Added ambient event fibers to cleanup");
        }

    }
}
