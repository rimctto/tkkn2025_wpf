using LiteDB;
using System.Numerics;
using tkkn2025.GameObjects.LevelMechanics.ParticleSprites;

namespace tkkn2025.Core.Behaviors;

public class ChaseBehavior : IBehavior
{

    private float deltaTime = 0;
    private float turnSpeed = 0;
    private float speed = 0;
    private readonly Entity entity;
    private readonly Entity target;

    public ChaseBehavior(Entity entity, Entity target, float turnSpeed, float speed)
    {
        this.entity = entity;
        this.target = target;
        this.turnSpeed = turnSpeed;
        this.speed = speed;
    }

    public void ApplyUpdate(double deltaTime)
    {

        var velocity = entity.Velocity;
        var position = entity.Position;
        var targetPositoin = target.Position;
        var maxSpeed = 100f;

        speed += 10;
        speed = Math.Min(speed, maxSpeed);

        // 1. Compute the desired direction
        Vector2 toTarget = targetPositoin - position;
        if (toTarget.Length() > 0.01f) // Avoid division by zero
        {
            float speed2D = Math.Min((float)Math.Sqrt(velocity.X * velocity.X + velocity.Y * velocity.Y), maxSpeed);
            speed2D += 100;
            speed2D = Math.Min(speed2D, maxSpeed);

            Vector2 desired = Vector2.Normalize(toTarget) * speed;

            // 2. Compute the steering (desired velocity - current velocity)
            Vector2 steer = desired - velocity;

            // 3. Calculate target angle directly from position to target
            float targetAngle = MathF.Atan2(toTarget.Y, toTarget.X);
            float currentAngle = entity.Rotation;

            // Compute smallest angle difference for smooth rotation
            float deltaAngle = WrapAngle(targetAngle - currentAngle);

            // Limit the rotation speed
            float maxTurn = turnSpeed * (float)deltaTime;
            float turn = Clamp(deltaAngle, -maxTurn, maxTurn);

            float newAngle = currentAngle + turn;

            // Update entity rotation to face the target
            entity.Rotation = newAngle;

            // Update velocity based on the new rotation angle
            velocity = new Vector2(MathF.Cos(newAngle), MathF.Sin(newAngle)) * speed2D;
        }

        // 4. Move particle
        position += velocity * (float)deltaTime;

        entity.Velocity = velocity;
        entity.Position = position;

    }


    private float Clamp(float value, float min, float max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }

    private float WrapAngle(float angle)
    {
        while (angle < -MathF.PI) angle += 2 * MathF.PI;
        while (angle > MathF.PI) angle -= 2 * MathF.PI;
        return angle;
    }
}

