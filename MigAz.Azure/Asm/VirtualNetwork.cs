﻿using MigAz.Azure.Arm;
using MigAz.Core.ArmTemplate;
using MigAz.Core.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

namespace MigAz.Azure.Asm
{
    public class VirtualNetwork : IVirtualNetwork
    {
        #region Variables

        private AzureContext _AzureContext = null;
        private XmlNode _XmlNode = null;
        private AffinityGroup _AsmAffinityGroup = null;
        private List<ISubnet> _AsmSubnets = null;
        private VirtualNetworkGateway _AsmVirtualNetworkGateway = null;
        private List<VirtualNetworkGateway> _AsmVirtualNetworkGateways2 = null;
        private List<LocalNetworkSite> _AsmLocalNetworkSites = null;
        private List<ClientRootCertificate> _AsmClientRootCertificates = null;
        private String _TargetName = String.Empty;

        #endregion

        #region Constructors

        private VirtualNetwork() { }

        public VirtualNetwork(AzureContext azureContext, XmlNode xmlNode)
        {
            _AzureContext = azureContext;
            _XmlNode = xmlNode;

            this.TargetName = this.Name;
        }

        public async Task InitializeChildrenAsync()
        {
            if (_XmlNode.SelectSingleNode("AffinityGroup") != null)
                _AsmAffinityGroup = await _AzureContext.AzureRetriever.GetAzureAsmAffinityGroup(_XmlNode.SelectSingleNode("AffinityGroup").InnerText);

            _AsmSubnets = new List<ISubnet>();
            foreach (XmlNode subnetNode in _XmlNode.SelectNodes("Subnets/Subnet"))
            {
                Subnet asmSubnet = new Subnet(_AzureContext, this, subnetNode);
                await asmSubnet.InitializeChildrenAsync();
                _AsmSubnets.Add(asmSubnet);
            }

            _AsmVirtualNetworkGateway = await _AzureContext.AzureRetriever.GetAzureAsmVirtualNetworkGateway(this);

            _AsmLocalNetworkSites = new List<LocalNetworkSite>();
            foreach (XmlNode localNetworkSiteXml in _XmlNode.SelectNodes("Gateway/Sites/LocalNetworkSite"))
            {
                LocalNetworkSite asmLocalNetworkSite = new LocalNetworkSite(_AzureContext, this, localNetworkSiteXml);
                await asmLocalNetworkSite.InitializeChildrenAsync();
                _AsmLocalNetworkSites.Add(asmLocalNetworkSite);
            }

            _AsmClientRootCertificates = await _AzureContext.AzureRetriever.GetAzureAsmClientRootCertificates(this);
        }

        #endregion

        #region Properties

        public string Name
        {
            get { return _XmlNode.SelectSingleNode("Name").InnerText; }
        }

        public string TargetName
        {
            get { return _TargetName; }
            set { _TargetName = value.Replace(" ", String.Empty); }
        }

        public string Id
        {
            get { return this.Name; }
        }

        public string TargetId
        {
            get { return "[concat(" + ArmConst.ResourceGroupId + ", '" + ArmConst.ProviderVirtualNetwork + this.GetFinalTargetName() + "')]"; }
        }

        public string Location
        {
            get
            {
                if (_XmlNode.SelectSingleNode("Location") != null)
                    return _XmlNode.SelectSingleNode("Location").InnerText;
                else if (this.AffinityGroup != null)
                    return this.AffinityGroup.Location;
                else
                    return String.Empty;

            }
        }

        public AffinityGroup AffinityGroup
        {
            get { return _AsmAffinityGroup; }
        }

        public List<string> AddressPrefixes
        {
            get {

                List<string> addressprefixes = new List<string>();
                foreach (XmlNode addressprefix in _XmlNode.SelectNodes("AddressSpace/AddressPrefixes"))
                {
                    addressprefixes.Add(addressprefix.SelectSingleNode("AddressPrefix").InnerText);
                }

                return addressprefixes;
            }
        }

        public List<string> DnsServers
        {
            get
            { 
                List<string> dnsServers = new List<string>();
                foreach (XmlNode dnsserver in _XmlNode.SelectNodes("Dns/DnsServers/DnsServer"))
                {
                    dnsServers.Add(dnsserver.SelectSingleNode("Address").InnerText);
                }

                return dnsServers;
            }
        }

        public List<ISubnet> Subnets
        {
            get
            {
                return _AsmSubnets;
            }
        }

        public VirtualNetworkGateway Gateway
        {
            get { return _AsmVirtualNetworkGateway; }
            set { _AsmVirtualNetworkGateway = value; } // set was only allowed because of unit test, recommend getting rid of in future
        }
         
        public List<VirtualNetworkGateway> Gateways2
        {
            get { return _AsmVirtualNetworkGateways2; }
        }

        public bool HasNonGatewaySubnet
        {
            get
            {
                if ((this.Subnets.Count() == 0) ||
                    ((this.Subnets.Count() == 1) && (this.Subnets[0].Name == ArmConst.GatewaySubnetName)))
                    return false;

                return true;
            }
        }

        public bool HasGatewaySubnet
        {
            get
            {
                foreach (Subnet asmSubnet in this.Subnets)
                {
                    if (asmSubnet.Name == ArmConst.GatewaySubnetName)
                        return true;
                }
                return false;
            }
        }

        public List<LocalNetworkSite> LocalNetworkSites
        {
            get { return _AsmLocalNetworkSites; }
        }

        public List<String> VPNClientAddressPrefixes
        {
            get
            {
                List<String> addressPrefixes = new List<string>();

                foreach (XmlNode addressprefix in _XmlNode.SelectNodes("Gateway/VPNClientAddressPool/AddressPrefixes/AddressPrefix"))
                {
                    addressPrefixes.Add(addressprefix.InnerText);
                }

                return addressPrefixes;
            }
        }

        public List<ClientRootCertificate> ClientRootCertificates
        {
            get { return _AsmClientRootCertificates; }
        }

        #endregion

        #region Methods

        public override string ToString()
        {
            return this.TargetName;
        }

        public string GetFinalTargetName()
        {
            return this.TargetName + this._AzureContext.SettingsProvider.VirtualNetworkSuffix;
        }

        #endregion

    }
}
