﻿// ----------------------------------------------------------------------------------
//
// Copyright Microsoft Corporation
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// ----------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Management.Automation;
using System.Threading;
using Microsoft.Azure.Commands.RecoveryServices.SiteRecovery;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Management.SiteRecovery.Models;

namespace Microsoft.Azure.Commands.RecoveryServices
{
    /// <summary>
    /// Used to initiate a commit operation.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Start, "AzureSiteRecoveryTestFailoverJob", DefaultParameterSetName = ASRParameterSets.ByRPObject)]
    [OutputType(typeof(ASRJob))]
    public class StartAzureSiteRecoveryTestFailoverJob : RecoveryServicesCmdletBase
    {
        #region Parameters
        /// <summary>
        /// Job response.
        /// </summary>
        private JobResponse jobResponse = null;

        /// <summary>
        /// Network ID.
        /// </summary>
        private string networkId = string.Empty;

        /// <summary>
        /// Network Type (Logical network or VM network).
        /// </summary>
        private string networkType = string.Empty;

        /// <summary>
        /// Gets or sets ID of the Recovery Plan.
        /// </summary>
        [Parameter(ParameterSetName = ASRParameterSets.ByRPId, Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public string RpId {get; set;}

        /// <summary>
        /// Gets or sets Recovery Plan object.
        /// </summary>
        [Parameter(ParameterSetName = ASRParameterSets.ByRPObject, Mandatory = true, ValueFromPipeline = true)]
        [ValidateNotNullOrEmpty]
        public ASRRecoveryPlan RecoveryPlan {get; set;}

        /// <summary>
        /// Gets or sets failover direction for the recovery plan.
        /// </summary>
        [Parameter(ParameterSetName = ASRParameterSets.ByRPObject, Mandatory = true)]
        [Parameter(ParameterSetName = ASRParameterSets.ByRPId, Mandatory = true)]
        [Parameter(ParameterSetName = ASRParameterSets.ByPEObject, Mandatory = true)]
        [Parameter(ParameterSetName = ASRParameterSets.ByPEId, Mandatory = true)]
        [ValidateSet(
          PSRecoveryServicesClient.PrimaryToRecovery,
          PSRecoveryServicesClient.RecoveryToPrimary)]
        public string Direction {get; set;}

        /// <summary>
        /// Gets or sets ID of the PE.
        /// </summary>
        [Parameter(ParameterSetName = ASRParameterSets.ByPEId, Mandatory = true)]
        [Parameter(ParameterSetName = ASRParameterSets.ByPEIdWithLogicalNetworkID, Mandatory = true)]
        [Parameter(ParameterSetName = ASRParameterSets.ByPEIdWithVMNetworkID, Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public string ProtectionEntityId {get; set;}

        /// <summary>
        /// Gets or sets ID of the Recovery Plan.
        /// </summary>
        [Parameter(ParameterSetName = ASRParameterSets.ByPEId, Mandatory = true)]
        [Parameter(ParameterSetName = ASRParameterSets.ByPEIdWithLogicalNetworkID, Mandatory = true)]
        [Parameter(ParameterSetName = ASRParameterSets.ByPEIdWithVMNetworkID, Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public string ProtectionContainerId {get; set;}

        /// <summary>
        /// Gets or sets Protection Entity object.
        /// </summary>
        [Parameter(ParameterSetName = ASRParameterSets.ByPEObject, Mandatory = true, ValueFromPipeline = true)]
        [Parameter(ParameterSetName = ASRParameterSets.ByPEObjectWithLogicalNetworkID, Mandatory = true)]
        [Parameter(ParameterSetName = ASRParameterSets.ByPEObjectWithVMNetworkID, Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public ASRProtectionEntity ProtectionEntity {get; set;}

        /// <summary>
        /// Gets or sets switch parameter. This is required to wait for job completion.
        /// </summary>
        [Parameter]
        public SwitchParameter WaitForCompletion {get; set;}

        /// <summary>
        /// Gets or sets Logical network ID.
        /// </summary>
        [Parameter(ParameterSetName = ASRParameterSets.ByPEObjectWithLogicalNetworkID, Mandatory = true)]
        [Parameter(ParameterSetName = ASRParameterSets.ByPEIdWithLogicalNetworkID, Mandatory = true)]
        public string LogicalNetworkId {get; set;}

        /// <summary>
        /// Gets or sets VM network ID.
        /// </summary>
        [Parameter(ParameterSetName = ASRParameterSets.ByPEObjectWithVMNetworkID, Mandatory = true)]
        [Parameter(ParameterSetName = ASRParameterSets.ByPEIdWithVMNetworkID, Mandatory = true)]
        public string VmNetworkId {get; set;}
        #endregion Parameters

        /// <summary>
        /// ProcessRecord of the command.
        /// </summary>
        public override void ExecuteCmdlet()
        {
            try
            {
                switch (this.ParameterSetName)
                {
                    case ASRParameterSets.ByRPObject:
                        this.RpId = this.RecoveryPlan.ID;
                        this.StartRpTestFailover();
                        break;
                    case ASRParameterSets.ByRPId:
                        this.StartRpTestFailover();
                        break;
                    case ASRParameterSets.ByPEObject:
                        this.networkType = "DisconnectedVMNetworkTypeForTestFailover";
                        this.UpdateRequiredParametersAndStartFailover();
                        break;
                    case ASRParameterSets.ByPEObjectWithLogicalNetworkID:
                        this.networkType = "CreateVMNetworkTypeForTestFailover";
                        this.networkId = this.LogicalNetworkId;
                        this.UpdateRequiredParametersAndStartFailover();
                        break;
                    case ASRParameterSets.ByPEObjectWithVMNetworkID:
                        this.networkType = "UseVMNetworkTypeForTestFailover";
                        this.networkId = this.VmNetworkId;
                        this.UpdateRequiredParametersAndStartFailover();
                        break;
                    case ASRParameterSets.ByPEId:
                        this.StartPETestFailover();
                        break;
                    case ASRParameterSets.ByPEIdWithLogicalNetworkID:
                        this.networkType = "CreateVMNetworkTypeForTestFailover";
                        this.networkId = this.LogicalNetworkId;
                        this.StartPETestFailover();
                        break;
                    case ASRParameterSets.ByPEIdWithVMNetworkID:
                        this.networkType = "UseVMNetworkTypeForTestFailover";
                        this.networkId = this.VmNetworkId;
                        this.StartPETestFailover();
                        break;
                }
            }
            catch (Exception exception)
            {
                this.HandleException(exception);
            }
        }

        /// <summary>
        /// Starts RP test failover.
        /// </summary>
        private void StartRpTestFailover()
        {
            RpTestFailoverRequest recoveryPlanTestFailoverRequest = new RpTestFailoverRequest();
            recoveryPlanTestFailoverRequest.FailoverDirection = this.Direction;
            this.jobResponse = RecoveryServicesClient.StartAzureSiteRecoveryTestFailover(
                this.RpId, 
                recoveryPlanTestFailoverRequest);

            this.WriteJob(this.jobResponse.Job);

            if (this.WaitForCompletion.IsPresent)
            {
                this.WaitForJobCompletion(this.jobResponse.Job.ID);
            }
        }

        /// <summary>
        /// Starts PE Test failover.
        /// </summary>
        private void StartPETestFailover()
        {
            var tfoReqeust = new TestFailoverRequest();
            tfoReqeust.NetworkID = this.networkId;
            tfoReqeust.FailoverDirection = this.Direction;
            tfoReqeust.NetworkType = this.networkType;
            tfoReqeust.ReplicationProvider = string.Empty;
            tfoReqeust.ReplicationProviderSettings = string.Empty;

            this.jobResponse =
                RecoveryServicesClient.StartAzureSiteRecoveryTestFailover(
                this.ProtectionContainerId,
                this.ProtectionEntityId,
                tfoReqeust);
            this.WriteJob(this.jobResponse.Job);

            if (this.WaitForCompletion.IsPresent)
            {
                this.WaitForJobCompletion(this.jobResponse.Job.ID);
            }
        }

        /// <summary>
        /// Updates required parameters and starts test failover.
        /// </summary>
        private void UpdateRequiredParametersAndStartFailover()
        {
            if (!this.ProtectionEntity.Protected)
            {
                throw new InvalidOperationException(
                    string.Format(
                    Properties.Resources.ProtectionEntityNotProtected,
                    this.ProtectionEntity.Name));
            }

            this.ProtectionContainerId = this.ProtectionEntity.ProtectionContainerId;
            this.ProtectionEntityId = this.ProtectionEntity.ID;
            this.StartPETestFailover();
        }

        /// <summary>
        /// Writes Job.
        /// </summary>
        /// <param name="job">JOB object</param>
        private void WriteJob(Microsoft.WindowsAzure.Management.SiteRecovery.Models.Job job)
        {
            this.WriteObject(new ASRJob(job));
        }
    }
}