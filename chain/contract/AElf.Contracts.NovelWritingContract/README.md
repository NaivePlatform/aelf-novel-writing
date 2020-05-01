由于发布到区块链的文字会直接公开，等于默认让人白嫖，因此不会把全部文本放在链上。仅试读部分上链，这部分后期可增删、修改。
考虑付费阅读场景，接下来简单梳理一下逻辑。

# Version 0.1

核心上链数据：作者和章节信息，读者的订阅情况。

1. 作者使用CreateNewSet创建文集或者新书（该步骤以后得实现为通过提案），随后发布章节，前端为该章节准备好唯一Id（可以使url的格式），发送Publish交易，如果为试读章节，所有文文本全部存进区块链，非试读章节的所有文本存入中心化数据库，链上仅记录哈希。同时抛出NovelPublished事件。

2. 中心化服务器的扫链程序收到NovelPublished事件，将章节更新推送给订阅的读者的客户端。

3. 读者点击阅读，此时客户端使用读者的账户发送Subscribe交易，该交易参数包括章节Id。

4. 中心化服务器第一次为客户端发送章节全文之前，先通过IsSubscribed方法查询一下是否有订阅，如果返回true，就记录到本地，以后就无须查询了。

除此之外，另外提供了修改已发布文章、打赏、查询积分等方法。

该版本方案的Proto文件定义为：

```
syntax = "proto3";

import "aelf/core.proto";
import "aelf/options.proto";
import "google/protobuf/empty.proto";
import "google/protobuf/timestamp.proto";
import "acs5.proto";
import "acs9.proto";
import "acs10.proto";

option csharp_namespace = "AElf.Contracts.NovelWritingContract";

service NovelWritingContract {
    option (aelf.csharp_state) = "AElf.Contracts.NovelWritingContract.NovelWritingContractState";
    option (aelf.base) = "acs5.proto";
    option (aelf.base) = "acs9.proto";
    option (aelf.base) = "acs10.proto";
    
    rpc Initialize (InitializeInput) returns (google.protobuf.Empty) {
    }
    
    // For Writers
    rpc CreateNewSet (CreateNewSetInput) returns (aelf.Hash) {
    }
    rpc Publish (PublishInput) returns (aelf.Hash) {
    }
    rpc Edit (EditInput) returns (google.protobuf.Empty) {
    }
    
    // For Readers
    rpc Subscribe (SubscribeInput) returns (google.protobuf.Empty) {
    }
    rpc Reward (RewardInput) returns (google.protobuf.Empty) {
    }
    
    // Views
    rpc GetAdminAddress (google.protobuf.Empty) returns (aelf.Address) {
        option (aelf.is_view) = true;
    }
    rpc IsSubscribed (IsSubscribedInput) returns (google.protobuf.BoolValue) {
        option (aelf.is_view) = true;
    }
    rpc GetProfile (aelf.Address) returns (Profile) {
        option (aelf.is_view) = true;
    }
}

message InitializeInput {
    aelf.Address admin_address = 1;
    string writer_token_symbol = 2;// 作者操作需要消耗的代币
    string subscribe_token_symbol = 3;// 读者订阅需要消耗的代币
}

message CreateNewSetInput {
    string set_name = 1;
}

message PublishInput {
    aelf.Hash set_id = 1;
    string novel_name = 2;
    aelf.Hash novel_text_hash = 3;// 文章整体的哈希值，也可用于版权验证
    int32 length = 4;// 文章长度，用于计算收费数额
    string text = 5;// 文章全文（可选）
}

message EditInput {
    aelf.Hash set_id = 1;
    aelf.Hash novel_id = 2;// Publish后文章会获得唯一Id，见Publish交易的返回值
    aelf.Hash novel_test_hash = 3;
    string text = 4;
}

message SubscribeInput {
    aelf.Hash novel_id = 1;
    int64 willing_amount = 2;// 可选付费多少，多出的部分算作打赏
}

message RewardInput {
    aelf.Hash novel_id = 1;
    int64 amount = 2;// 不订阅，只打赏
}

message IsSubscribedInput {
    aelf.Address user_address = 1;
    aelf.Hash novel_id = 2;
}

// Data Structures.

message NovelSetInfo {
    aelf.Hash set_id = 1;
    string set_name = 2;
    google.protobuf.Timestamp create_time = 3;
    int64 novel_count = 4;
    aelf.Address set_owner = 5;
}

message NovelInfo {
    aelf.Hash set_id = 1;
    aelf.Hash novel_id = 2;
    string novel_name = 3;
    aelf.Address publisher_address = 4;
    aelf.Hash novel_text_hash = 5;
    int32 length = 6;
    string text = 7;
    google.protobuf.Timestamp publish_time = 8;
    google.protobuf.Timestamp latest_edit_time = 9;
}

message Profile {
    aelf.Address address = 1;
    google.protobuf.Timestamp first_publish_time = 2;
    google.protobuf.Timestamp first_subscribe_time = 3;
    int64 points = 4;// 订阅可以获得积分，只是暂时还用不到
}
```