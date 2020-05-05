using System;
using System.Linq;
using Acs10;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.NovelWritingContract
{
    public class NovelWritingContract : NovelWritingContractContainer.NovelWritingContractBase
    {
        public override Empty Initialize(InitializeInput input)
        {
            Assert(State.AdminAddress.Value == null, "Already initialized.");
            State.AdminAddress.Value = input.AdminAddress;
            State.WriterTokenSymbol.Value = input.WriterTokenSymbol;
            State.SubscribeTokenSymbol.Value = input.SubscribeTokenSymbol;

            State.TokenContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
            return new Empty();
        }

        public override Hash CreateNewSet(CreateNewSetInput input)
        {
            // TODO: Check permissions.

            var setId = Context.GenerateId(Context.Self,
                HashHelper.ConcatAndCompute(HashHelper.ComputeFrom(Context.Sender),
                    HashHelper.ComputeFrom(input.SetName)));

            var currentSetList = State.NovelSetList[Context.Sender] ?? new SymbolList();
            if (currentSetList.Value.Any())
            {
                Assert(currentSetList.Value.Contains(input.SetName), "Set name already exists.");
            }

            currentSetList.Value.Add(input.SetName);
            State.NovelSetList[Context.Sender] = currentSetList;

            State.NovelSetInfos[setId] = new NovelSetInfo
            {
                SetId = setId,
                SetName = input.SetName,
                CreateTime = Context.CurrentBlockTime,
                SetOwner = Context.Sender
            };
            return setId;
        }

        public override Hash Publish(PublishInput input)
        {
            // TODO: Maybe need to check permission.

            var novelId = Context.GenerateId(Context.Self,
                HashHelper.ConcatAndCompute(HashHelper.ComputeFrom(Context.Sender),
                    HashHelper.ComputeFrom(input.NovelTextHash)));

            Assert(State.NovelInfos[novelId] == null, "Novel already exists.");

            var setInfo = State.NovelSetInfos[input.SetId];
            if (setInfo == null)
            {
                throw new AssertionException($"Set with Id {input.SetId} not found.");
            }

            var rank = setInfo.NovelCount.Add(1);
            setInfo.NovelCount = rank;
            State.NovelHashes[input.SetId][rank] = novelId;
            State.NovelSetInfos[input.SetId] = setInfo;

            State.NovelInfos[novelId] = new NovelInfo
            {
                SetId = input.SetId,
                Length = input.Length,
                NovelId = novelId,
                NovelName = input.NovelName,
                NovelTextHash = input.NovelTextHash,
                PublisherAddress = Context.Sender,
                PublishTime = Context.CurrentBlockTime,
                LatestEditTime = Context.CurrentBlockTime,
                Text = input.Text
            };

            var profile = State.Profiles[Context.Sender];
            if (profile == null)
            {
                profile = new Profile
                {
                    Address = Context.Sender,
                    FirstPublishTime = Context.CurrentBlockTime,
                };
            }
            else
            {
                if (profile.FirstPublishTime == null)
                {
                    profile.FirstPublishTime = Context.CurrentBlockTime;
                }
            }

            State.Profiles[Context.Sender] = profile;

            Context.Fire(new NovelPublished
            {
                SetId = input.SetId,
                Length = input.Length,
                NovelId = novelId,
                NovelName = input.NovelName,
                NovelTextHash = input.NovelTextHash,
                PublisherAddress = Context.Sender,
                PublishTime = Context.CurrentBlockTime,
            });

            return novelId;
        }

        public override Empty Edit(EditInput input)
        {
            var setInfo = State.NovelSetInfos[input.SetId];
            if (setInfo == null)
            {
                throw new AssertionException($"Set with Id {input.SetId} not found.");
            }

            var novelInfo = State.NovelInfos[input.NovelId];
            novelInfo.Text = input.Text;
            novelInfo.NovelTextHash = input.NovelTestHash;
            novelInfo.LatestEditTime = Context.CurrentBlockTime;
            State.NovelInfos[input.NovelId] = novelInfo;

            return new Empty();
        }

        public override Empty Subscribe(SubscribeInput input)
        {
            // Calculate amount.
            var novelInfo = State.NovelInfos[input.NovelId];
            var length = novelInfo.Length;
            var amount = length.Mul(State.UnitPrice.Value).Div(1000);
            var actualAmount = Math.Max(amount, input.WillingAmount);

            // Transfer tokens from user.
            State.TokenContract.TransferFrom.Send(new TransferFromInput
            {
                From = Context.Sender,
                To = Context.Self,
                Amount = actualAmount,
                Symbol = State.SubscribeTokenSymbol.Value
            });

            if (actualAmount > amount)
            {
                var currentReward = State.RewardStatus[Context.Sender][novelInfo.PublisherAddress];
                State.RewardStatus[Context.Sender][novelInfo.PublisherAddress] =
                    currentReward.Add(actualAmount.Sub(amount));
            }

            var income = State.Income[input.NovelId];
            State.Income[input.NovelId] = income.Add(actualAmount);
            State.SubscribeStatus[Context.Sender][input.NovelId] = true;

            var profile = State.Profiles[Context.Sender];
            if (profile == null)
            {
                profile = new Profile
                {
                    Address = Context.Sender,
                    FirstSubscribeTime = Context.CurrentBlockTime,
                    Points = actualAmount
                };
            }
            else
            {
                if (profile.FirstSubscribeTime == null)
                {
                    profile.FirstSubscribeTime = Context.CurrentBlockTime;
                }

                profile.Points = profile.Points.Add(actualAmount);
            }

            State.Profiles[Context.Sender] = profile;

            return new Empty();
        }

        public override Empty Reward(RewardInput input)
        {
            var novelInfo = State.NovelInfos[input.NovelId];

            // Transfer tokens from user.
            State.TokenContract.TransferFrom.Send(new TransferFromInput
            {
                From = Context.Sender,
                To = Context.Self,
                Amount = input.Amount,
                Symbol = State.SubscribeTokenSymbol.Value
            });

            var currentReward = State.RewardStatus[Context.Sender][novelInfo.PublisherAddress];
            State.RewardStatus[Context.Sender][novelInfo.PublisherAddress] =
                currentReward.Add(input.Amount);

            return new Empty();
        }

        public override BoolValue IsSubscribed(IsSubscribedInput input)
        {
            return new BoolValue
            {
                Value = State.SubscribeStatus[input.UserAddress][input.NovelId]
            };
        }

        public override Address GetAdminAddress(Empty input)
        {
            return State.AdminAddress.Value;
        }

        public override Profile GetProfile(Address input)
        {
            return State.Profiles[input];
        }
    }
}