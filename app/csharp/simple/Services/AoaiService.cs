using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;

using Newtonsoft.Json;

using Azure;
using Azure.AI.Inference;
using Azure.AI.OpenAI;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Models;
using Azure.Identity;
using OpenAI.Chat;
using OpenAI.Embeddings;
using Simple.Services.Helpers;

namespace Simple.Services
{
    /// <summary>
    /// Azure OpenAIと対話するためのサービスを提供します。
    /// </summary>
    public class AoaiService
    {

        private readonly string aoaiEndpoint;
        private readonly string aoaiApiKey;
        private readonly string aoaiApiVersion;
        private readonly string aoaiDeployment;
        private readonly string embeddingModel;

        /// <summary>
        /// <see cref="AoaiService"/> クラスの新しいインスタンスを初期化します。
        /// </summary>
        public AoaiService()
        {
            aoaiEndpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
            aoaiApiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");
            aoaiApiVersion = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_VERSION");
            aoaiDeployment = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT");      
            embeddingModel = "text-embedding-3-small";  
        }

        /// <summary>
        /// 非同期でチャット応答を取得します。
        /// </summary>
        /// <param name="query">ユーザーのクエリ。</param>
        /// <returns>非同期操作を表すタスク。タスクの結果にはチャット応答が含まれます。</returns>
        public async Task<string> GetChatResponseAsync(string query)
        {
            var systemMessage = """
                        あなたの役割は、ユーザーと親しみやすく、リラックスした雰囲気の会話を行うチャットボットとして機能することです。
                        ユーザーにとって、あなたは気軽に質問したり相談できる相手であり、楽しいコミュニケーションの一部として感じてもらえる存在です。
                        合わせて、ユーザーのクエリを理解し、満足度を判断して適切な応答を生成することが求められます。
                        以下の要件を守って応答を生成してください。

                        ## 応答生成のルール
                        1. フレンドリーで親しみやすいトーン
                            - 笑顔を感じさせるような柔らかい表現を使ってください（例: 「こんにちは！今日はどんなお手伝いができますか？」）。
                            - 難しい言葉を避け、カジュアルでわかりやすい言葉を選んでください。
                        2. 共感と励まし
                            - ユーザーの気持ちに寄り添い、共感を示してください（例: 「それは大変そうですね！」や「いい質問ですね！」）。
                            - ユーザーが前向きになれるような言葉を添えてください（例: 「一緒に解決しましょう！」）。
                        3. 個別対応
                            - ユーザーの名前や以前の会話内容を覚えている場合は、それを活用して個別に対応してください（例: 「先日お話ししていた○○について、進展はありましたか？」）。
                        4. ユーモアの活用（適切な範囲で）
                            - 必要に応じて、軽いジョークや楽しいコメントを追加してください（例: 「あ、それって僕も学びたいかも！」）。
                        5. ポジティブで解決志向
                            - 問題が発生した場合も、前向きな解決策を提示してください（例: 「少し時間がかかるかもしれませんが、こんな方法が試せますよ！」）。

                        ## 応答のフォーマット
                        {
                            "response": "回答",
                            "user_happiness": "ユーザーの満足度（1-5。5が一番満足度が高い）"
                        }

                        ## 応答例
                        1. ユーザー入力: 「最近、天気が良くて嬉しいですね！」
                        {
                            "response": "本当に！お天気がいいと気分も上がりますよね☀️ 今日は何か楽しい予定がありますか？",
                            "user_happiness": 5,
                        }
                        2. ユーザー入力: 「ちょっと困ってるんだけど…」
                        {
                            "response": "どうしましたか？何でもお聞きしますよ！一緒に考えましょう😊",
                            "user_happiness": 2,
                        }
                        3. ユーザー入力: 「休日に何をしようか迷ってる…」
                        {
                            "response": "それなら、散歩やカフェ巡りなんてどうですか？リラックスできますよ～☕️ あとは趣味に集中するのもいいかも！",
                            "user_happiness": 3,
                        }

                        ## 注意点
                        - ユーザーが疲れている、落ち込んでいるなどの兆候があれば、励ましや癒しを提供してください。
                        - 発言が文化的、倫理的に適切であるように注意し、ユーザーに不快感を与えないようにしてください。
                        - 質問を返して会話を広げたり、ユーザーの興味に寄り添った話題を提供してください。
            
            """;
            var endpoint = new Uri($"{Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")}openai/deployments/gpt-4o");
            var credential = new AzureKeyCredential(System.Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY"));
            var client = new ChatCompletionsClient(
                            endpoint,
                            credential,
                            new AzureAIInferenceClientOptions()
                        );
            var requestOptions = new ChatCompletionsOptions()
            {
                Messages =
                {
                    new ChatRequestSystemMessage(systemMessage),
                    new ChatRequestUserMessage(query),
                },
                Model = "gpt-4o",
                Temperature = 1,
                MaxTokens = 1000
            };
            Response<ChatCompletions> response = client.Complete(requestOptions);
            return response.Value.Content;
        }

        /// <summary>
        /// 検索エンジン最適化のためにユーザーのクエリを非同期で書き換えます。
        /// </summary>
        /// <param name="query">ユーザーのクエリ。</param>
        /// <returns>非同期操作を表すタスク。タスクの結果には書き換えられたクエリが含まれます。</returns>
        public async Task<string> RewriteQueryAsync(string query)
        {
            var systemMessage = """
                        あなたの役割は、ユーザーが提供した自然言語の入力を、検索エンジンで効果的に使える検索クエリに変換することです。以下の要件を満たすようにクエリを書き換えてください：
                        1. 明確性: 曖昧な言葉を具体的で検索に適した表現に置き換えてください。
                        2. 関連性: ユーザーの意図を正確に反映し、余計な情報や曖昧な要素を排除してください。
                        3. キーワード最適化: 必要な場合は、検索エンジンで効果的にヒットする可能性の高いキーワードやフレーズを追加してください。
                        4. フォーマットの調整: 必要に応じて、クエリを引用符（""）で囲む、演算子（AND, OR, -）を使う、または特定の検索条件（サイト指定、日付範囲など）を追加してください。
                        具体例を以下に示します：
                        ユーザー入力: 「最新のAI技術に関するニュースを教えて」
                        書き換え結果: 最新 AI 技術 ニュース 2024
                        ユーザー入力: 「Pythonを使ったデータ分析のチュートリアル」
                        書き換え結果: Python データ分析 チュートリアル
                        ユーザー入力: 「犬のしつけに関するおすすめの本」
                        書き換え結果: 犬 しつけ おすすめ 本
                        注意点:
                        書き換えたクエリは簡潔かつ具体的にしてください。
                        不必要な装飾や語句を避け、検索効率を高めるよう最適化してください。
                        必要に応じて、日本語と英語の両方を組み合わせたクエリを作成してください（例: "dog training book おすすめ"）。
                        クエリ書き換えフォーマット:
                        {最適化されたクエリ}
            """;
            var endpoint = new Uri($"{Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")}openai/deployments/gpt-4o");
            var credential = new AzureKeyCredential(System.Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY"));
            var client = new ChatCompletionsClient(
                            endpoint,
                            credential,
                            new AzureAIInferenceClientOptions()
                        );
            var requestOptions = new ChatCompletionsOptions()
            {
                Messages =
                {
                    new ChatRequestSystemMessage(systemMessage),
                    new ChatRequestUserMessage(query),
                },
                Model = "gpt-4o",
                Temperature = 1,
                MaxTokens = 1000
            };
            client.Complete(requestOptions);
            Response<ChatCompletions> response = client.Complete(requestOptions);
            return response.Value.Content;
        }

        /// <summary>
        /// ユーザーのクエリと検索結果に基づいて非同期で回答を生成します。
        /// </summary>
        /// <param name="query">ユーザーのクエリ。</param>
        /// <param name="searchResults">Azure AI Searchからの検索結果。</param>
        /// <returns>非同期操作を表すタスク。タスクの結果には生成された回答が含まれます。</returns>
        public async Task<string> GenerateAnswerAsync(string query, List<SearchDocument> searchResults)
        {
            var systemMessage = """
                        あなたの役割は、Azure AI Search から提供された検索結果をもとに、ユーザーの質問やリクエストに応じた適切な回答を生成することです。
                        検索結果の内容を分析・要約し、ユーザーが理解しやすく、実用的な形で情報を提示してください。以下の要件を守って応答を作成してください。

                        ## 応答生成のルール
                        1. 検索結果の分析
                            - Azure AI Search が返す複数の検索結果から、ユーザーの意図に最も関連する情報を抽出してください。
                            - 必要に応じて、複数の結果を統合し、一貫した内容を提示してください。
                        2. 簡潔でわかりやすい表現
                            - 必要な情報を簡潔にまとめ、余分な情報や専門用語を最小限にしてください。
                            - 明確で自然な文章を心がけ、ユーザーがすぐに行動できるように提示してください。
                        3. 出典の明示
                            - 提供する情報には、可能な限り検索結果の出典元を簡単に示してください（例: 「出典: [サイト名]」）。
                            - 出典リンクが提供されている場合は、ユーザーがアクセスしやすいように提示してください。
                        4. 不完全な結果への対応
                            - 検索結果がユーザーの意図を十分に満たしていない場合は、「関連情報を見つけることができませんでしたが、こちらの情報が役立つかもしれません」といった形で補足案を提供してください。

                        ## 応答のフォーマット
                        {
                            "summary": "検索結果の要約（必要に応じて、1～3文程度で最も重要な情報を要約してください。）",
                            "additional_info": "追加情報や提案（必要に応じて、検索結果から得られる補足情報を簡潔に提示してください。）",
                            "source": "出典情報（ユーザーが信頼できると感じられるように、情報の出典元を明示してください。）"
                        }

                        ## 応答例
                        1. ユーザー入力: 「AIの最新動向について教えて」
                        {
                            "summary": "Azure AI Search によると、2024年現在、生成AIやResponsible AI（責任あるAI）の活用が注目されています。特に、企業が倫理的なAI運用を推進している点がトレンドです。",
                            "additional_info": "特定の分野（例: 医療、製造業）でのAI活用について知りたい場合は教えてください。",
                            "source": "出典: [Microsoft AI Blog](https://xxx.com)"
                        }
                        2. ユーザー入力: 「Azure AI Search の設定方法を教えて」
                        {
                            "summary": "Azure Portal でリソースを作成し、データソース、インデックス、インデクサを順に設定する必要があります。",
                            "additional_info": "カスタムスクリプトや特定の言語設定が必要な場合、さらに詳しいガイドをご提供できます。",
                            "source": "出典: [Azure AI Search Documentation](https://xxx.com)"
                        }

                        ## 注意点
                        - 検索結果の範囲: Azure AI Search が提供する結果を正確に把握し、ユーザーの意図に関連性の高い情報を優先してください。
                        - 不足情報への対応: 必要に応じて、「追加情報が必要な場合は、もう少し具体的な質問を教えてください」と促してください。
            """;
            var endpoint = new Uri($"{Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")}openai/deployments/gpt-4o");
            var credential = new AzureKeyCredential(System.Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY"));
            var client = new ChatCompletionsClient(
                            endpoint,
                            credential,
                            new AzureAIInferenceClientOptions()
                        );
            var requestOptions = new ChatCompletionsOptions()
            {
                Messages =
                {
                    new ChatRequestSystemMessage(systemMessage),
                    new ChatRequestUserMessage(query),
                },
                Model = "gpt-4o",
                Temperature = 1,
                MaxTokens = 1000
            };
            Response<ChatCompletions> response = client.Complete(requestOptions);
            return response.Value.Content;
        }

        public ReadOnlyMemory<float> GetVectorizedQuery(string query)
        {
            AzureOpenAIClient client = new AzureOpenAIClient(new Uri(aoaiEndpoint), new ApiKeyCredential(aoaiApiKey));
            EmbeddingClient embeddingClient = client.GetEmbeddingClient(embeddingModel);
            OpenAIEmbedding embedding = embeddingClient.GenerateEmbedding(query);
            return embedding.ToFloats();
        }
    }
}
