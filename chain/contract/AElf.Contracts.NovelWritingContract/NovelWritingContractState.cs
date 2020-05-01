using Acs10;
using AElf.Contracts.MultiToken;
using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AElf.Contracts.NovelWritingContract
{
    public class NovelWritingContractState : ContractState
    {
        internal TokenContractContainer.TokenContractReferenceState TokenContract { get; set; }

        /// <summary>
        /// Can be an individual or an organization.
        /// </summary>
        public SingletonState<Address> AdminAddress { get; set; }

        /// <summary>
        /// Price per 1000 chars.
        /// </summary>
        public SingletonState<int> UnitPrice { get; set; }

        public SingletonState<string> WriterTokenSymbol { get; set; }
        public SingletonState<string> SubscribeTokenSymbol { get; set; }

        public MappedState<Address, SymbolList> NovelSetList { get; set; }

        public MappedState<Hash, NovelSetInfo> NovelSetInfos { get; set; }

        public MappedState<Hash, long, Hash> NovelHashes { get; set; }

        public MappedState<Hash, NovelInfo> NovelInfos { get; set; }

        public MappedState<Address, Hash, bool> SubscribeStatus { get; set; }

        /// <summary>
        /// User Address -> Writer Address -> Reward Amount.
        /// </summary>
        public MappedState<Address, Address, long> RewardStatus { get; set; }

        public MappedState<Hash, long> Income { get; set; }

        public MappedState<Address, Profile> Profiles { get; set; }
    }
}