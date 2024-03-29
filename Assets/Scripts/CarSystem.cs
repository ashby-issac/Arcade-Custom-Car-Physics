using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

public class CarSystem : ICarComponents
{
    [Category("Global Attributes")]
    private Rigidbody carRigidbody;
    private CarSpecs carSpecs;

    [Category("SuspensionForce Attributes")]
    private float force;
    private float springOffset;
    private float velocitySpeed;
    private Vector3 springDir = default;
    private Vector3 pointVelocity = default;

    [Category("SteeringForce Attributes")]
    private float accel;
    private float steeringVel;
    private float changeInVel;
    private Vector3 tirePointVel;

    [Category("AccelerationForce Attributes")]
    private Vector3 accelForce;

    private Transform[] frontTireTransforms;

    public CarSystem(Rigidbody carRigidbody = null, CarSpecs carSpecs = null, Transform[] frontTireTransforms = null) 
    {
        this.carRigidbody = carRigidbody;
        this.carSpecs = carSpecs;
        this.frontTireTransforms = frontTireTransforms;

        GameplayController.Instance.OnApplyForce += ApplyCarForces;
        GameplayController.Instance.OnCarRotate += CarRotation;
    }

    /* Force that makes the rigidbody float on 
     * the ground using the raycast-based approach */
    public void SuspensionForce(float distance = 0, Transform tireTransform = null)
    {
        springDir = tireTransform.up;
        pointVelocity = carRigidbody.GetPointVelocity(tireTransform.position);

        springOffset = carSpecs.suspensionRestDist - distance;

        velocitySpeed = Vector3.Dot(springDir, pointVelocity);
        force = (springOffset * carSpecs.strength) - (velocitySpeed * carSpecs.dampingForce);

        carRigidbody.AddForceAtPosition(springDir * force, tireTransform.position);
    }

    /* Force required to avoid unnecessary slipping for the car
     * Can reduce traction using gripFactor
    */
    public void SteeringForce(Transform tireTransform = null)
    {
        tirePointVel = carRigidbody.GetPointVelocity(tireTransform.position);
        steeringVel = Vector3.Dot(tirePointVel, tireTransform.right);

        changeInVel = -steeringVel * carSpecs.gripFactor; 
        accel = changeInVel / Time.fixedDeltaTime;

        carRigidbody.AddForceAtPosition(tireTransform.right * accel * carSpecs.tireMass, tireTransform.position);
    }

    /* Forward/Backward force for the car's rigidbody */
    public void AccelerationForce(float accelInput = 0, Transform tireTransform = null)
    {
        if (accelInput != 0.0f)
        {
            accelForce = carSpecs.accelSpeed * accelInput * carRigidbody.transform.forward;
            carRigidbody.AddForceAtPosition(accelForce, tireTransform.position);
        }
    }

    /* Apply force for Suspension, Steering, and Acceleration */
    private void ApplyCarForces(float distance = 0, Transform tireTransform = null, float accelInput = 0)
    {
        SuspensionForce(distance, tireTransform);
        SteeringForce(tireTransform);
        AccelerationForce(accelInput, tireTransform);
    }

    /* Rotate the front wheel transforms */
    private void CarRotation(float steeringInput = 0)
    {
        foreach (Transform frontTire in frontTireTransforms)
        {
            float inputRotation = steeringInput * carSpecs.tireRotationAngle;
            Quaternion newRotation = Quaternion.Euler(frontTire.localEulerAngles.x, inputRotation, frontTire.localEulerAngles.z);
            frontTire.localRotation = Quaternion.Slerp(frontTire.localRotation, newRotation, Time.deltaTime * carSpecs.rotationSpeed);
        }
    }
}
