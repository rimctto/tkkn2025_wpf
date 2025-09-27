using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace tkkn2025.Core.Behaviors;

public class PositionBehavior: IBehavior
{
    private readonly Entity entity;

    public PositionBehavior(Entity entity)
    {
        this.entity = entity;
    }
    public void ApplyUpdate(double deltaTime)
    {
        Vector2 newVelocity = new Vector2();
        entity.Velocity = newVelocity;

        var newX = entity.X + (int)entity.Velocity.X;
        var newY = entity.Y + (int)entity.Velocity.Y;
                
    }

}
