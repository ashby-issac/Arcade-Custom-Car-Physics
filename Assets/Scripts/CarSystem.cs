using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

public class CarSystem : ICarComponents
{
    [Category("Global Attributes")] private Rigidbody carRigidbody;
    private CarSpecs carSpecs;

    [Category("Animation Curves")] private AnimationCurve steeringAnimCurve;
    private AnimationCurve accelAnimCurve;

    [Category("SuspensionForce Attributes")]
    private float force;

    private float springOffset;
    private float velocitySpeed;
    private Vector3 springDir = default;
    private Vector3 pointVelocity = default;

    [Category("SteeringForce Attributes")] private float accel;
    private float steeringVel;
    private float changeInVel;
    private Vector3 tirePointVel;

    [Category("AccelerationForce Attributes")]
    private Vector3 accelForce;

    private Transform[] frontTireTransforms;

    public CarSystem(Rigidbody carRigidbody = null, CarSpecs carSpecs = null, Transform[] frontTireTransforms = null,
        AnimationCurve steeringAnimCurve = null, AnimationCurve accelAnimCurve = null)
    {
        this.carRigidbody = carRigidbody;
        this.carSpecs = carSpecs;
        this.frontTireTransforms = frontTireTransforms;
        this.steeringAnimCurve = steeringAnimCurve;
        this.accelAnimCurve = accelAnimCurve;

        GameplayController.Instance.OnApplyForce += ApplyCarForces;
        GameplayController.Instance.OnCarRotate += CarRotation;
    }

    /* Force that makes the rigidbody float on
     * the ground using the raycast-based approach */
    public void SuspensionForce(float distance = 0, Transform tireTransform = null)
    {
        springDir = tireTransform.up;
        pointVelocity = carRigidbody.GetPointVelocity(tireTransform.position);
        springOffset = carSpecs.suspensionRestDist - distance; // 0.4
        velocitySpeed = Vector3.Dot(springDir, pointVelocity);
        
        // also dependent on the mass of the object.
        force = (springOffset * carSpecs.strength) - (velocitySpeed * carSpecs.dampingForce);

        // Debug.LogWarning($":: springOffset: {springOffset} :: velocitySpeed: {velocitySpeed} :: force: {force}");

        carRigidbody.AddForceAtPosition(springDir * force, tireTransform.position);
    }

    /* Force required to avoid unnecessary slipping for the car
     * Can reduce traction using gripFactor
     */
    public void SteeringForce(Transform tireTransform = null)
    {
        tirePointVel = carRigidbody.GetPointVelocity(tireTransform.position);
        steeringVel = Vector3.Dot(tirePointVel, tireTransform.right);

        //var velOnX = Mathf.Abs(Mathf.Clamp01(steeringVel));
        //var gripFactor = steeringAnimCurve.Evaluate(velOnX);
        //Debug.Log($":: gripFactor: {gripFactor}");

        changeInVel = -steeringVel * carSpecs.gripFactor;
        accel = changeInVel / Time.fixedDeltaTime;
        var forceToApply = accel * carSpecs.tireMass;
        
        carRigidbody.AddForceAtPosition(tireTransform.right * forceToApply, tireTransform.position);
    }

    /* Forward/Backward force for the car's rigidbody */
    public void AccelerationForce(float accelInput = 0, Transform tireTransform = null, Transform tireMesh = null)
    {
        if (accelInput != 0.0f)
        {
            float speed = Vector3.Dot(tireTransform.forward, carRigidbody.velocity);
            float clampedSpeed = Mathf.Clamp01(speed / carSpecs.totalSpeed);

            var accelSpeed = accelAnimCurve.Evaluate(clampedSpeed) * carSpecs.speedValue;

            accelForce = accelSpeed * accelInput * carRigidbody.transform.forward;
            carRigidbody.AddForceAtPosition(accelForce, tireTransform.position);
            
            tireMesh.Rotate(Vector3.right, accelSpeed);
        }
    }

    /* Apply force for Suspension, Steering, and Acceleration */
    private void ApplyCarForces(float distance = 0, Transform tireTransform = null, float accelInput = 0, Transform tireMesh = null)
    {
        SuspensionForce(distance, tireTransform);
        SteeringForce(tireTransform);
        AccelerationForce(accelInput, tireTransform, tireMesh);
    }

    /* Rotate the front wheel transforms */
    private void CarRotation(float steeringInput = 0)
    {
        float inputRotY = steeringInput * carSpecs.tireRotationAngle;

        foreach (Transform frontTire in frontTireTransforms)
        {
            Quaternion updatedRotation = Quaternion.Euler(frontTire.localEulerAngles.x, inputRotY,
                frontTire.localEulerAngles.z);

            frontTire.localRotation = steeringInput == 0 ? new Quaternion(0, 0, 0, 1) : Quaternion.Slerp(frontTire.localRotation, updatedRotation,
                Time.deltaTime * carSpecs.rotationSpeed);
        }
    }
}