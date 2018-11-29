using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rage;
using LSPD_First_Response.Mod.API;

namespace Traffic_Policer.Ambientevents
{
    
    internal class UnroadworthyVehicle : AmbientEvent
    {
        
        

        public UnroadworthyVehicle(Ped Driver, bool createBlip, bool showMessage) : base (Driver, createBlip, showMessage, "Creating unroadworthy vehicle event.")
        {
            MainLogic();
        }


        protected override void MainLogic()
        {
            AmbientEventMainFiber = GameFiber.StartNew(delegate
            {

                try {
                    
                    
                    
                    car.Windows[TrafficPolicerHandler.rnd.Next(3)].Remove();

                    car.Wheels[0].BurstTire();
                    car.FuelTankHealth = 70f;

                    car.EngineHealth = 60f;
                    if (TrafficPolicerHandler.IsLSPDFRPlusRunning)
                    {
                        API.LSPDFRPlusFunctions.AddQuestionToTrafficStop(driver, "What happened to your vehicle?", new List<string> { "It needs a trip to the garage, officer.", "It's getting a bit old.", "Someone slashed my tyres!", "What's wrong with it?" });
                    }
                    while (eventRunning)
                    {
                        GameFiber.Yield();
                        if (Functions.IsPlayerPerformingPullover())
                        {

                            eventRunning = false;
                            performingPullover = true;
                            break;
                        }
                        if (Vector3.Distance(Game.LocalPlayer.Character.Position, driver.Position) > 300f)
                        {
                            eventRunning = false;

                            break;
                        }
                    }

                    
                   
                    
                } catch (Exception e)
                {
                    if (driverBlip.Exists())
                    {
                        driverBlip.Delete();
                    }
                    if (car.Exists())
                    {
                        car.IsPersistent = false;
                    }
                    
                }
                finally
                {
                    End();
                }
            });

           
        }
    }
}
