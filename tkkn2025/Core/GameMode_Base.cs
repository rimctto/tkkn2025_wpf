using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tkkn2025.Settings;

namespace tkkn2025.Core;

public class GameSettings_Base
{


    public void SaveSettings()
    {

    }
    public void LoadSettings()
    {

    }

    }


public class GameMode_Base
{
    public string Name { get; set; }
    public string Description { get; set; }

    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public TimeSpan Duration => EndTime?.Subtract(StartTime) ?? TimeSpan.Zero;
    public int FinalParticleCount { get; set; }

    /// <summary>
    /// The game mode that was played for this game instance
    /// </summary>

    internal GameSettings_Base Settings { get; set; } = new GameSettings_Base();


}


public class GameSettings_Maze: GameSettings_Base
{

    //Settings
    public static DoubleSetting ShipSpeed_Normal { get; } = new DoubleSetting(
            name: nameof(ShipSpeed_Normal),
            displayName: "Ship Speed",
            category: "Movement",
            defaultValue: 200.0,
            min: 50.0,
            max: 500.0
        );

    public static DoubleSetting ShipSpeed_Boost { get; } = new DoubleSetting(
        name: nameof(ShipSpeed_Boost),
        displayName: "Ship Boost",
        category: "Movement",
        defaultValue: 75,
        min: 0.0,
        max: 150.0,
        description: "Ship speed value when shift key is pressed"
    );

    public static DoubleSetting ShipSpeed_SuperBoost { get; } = new DoubleSetting(
        name: nameof(ShipSpeed_SuperBoost),
        displayName: "Ship Super Boost",
        category: "Movement",
        defaultValue: 150,
        min: 0.0,
        max: 300,
        description: "Ship speed value when double-tapping and holding space key"
    );





}