import os
from dotenv import load_dotenv
from fastapi import FastAPI, Request
from fastapi.exceptions import RequestValidationError
from fastapi.openapi.docs import get_swagger_ui_html
from fastapi.openapi.utils import get_openapi
from fastapi.responses import JSONResponse
from starlette import status

from azure.monitor.opentelemetry import configure_azure_monitor

from swagger.model.chat_response import ChatResponse
from service.aoai_service import get_embedding_from_query, rewrite_query, generate_answer, chat

from service.ai_search_service import search_fulltext, search_vector


load_dotenv()


configure_azure_monitor(
    enable_live_metrics=True,
)


app = FastAPI(
    title="Simple API",
    description="API for Cloud Workshop Simple App",
    version="1.0.0",
)


@app.get("/health")
def get_health():
    """
    ヘルスチェックエンドポイント。

    Returns:
        dict: アプリケーションのステータスを含む辞書。
    """
    return {"status": "ok"}


@app.get(
    "/chat",
    responses={
        status.HTTP_200_OK: {
            "description": "Successful Response",
            "model": ChatResponse,
        },
    }
)
def chat_handler(query: str):
    """
    クエリを処理し、回答を返すチャットエンドポイント。

    Args:
        query (str): 処理するクエリ文字列。

    Returns:
        dict: クエリに対する回答を含む辞書。
    """
    answer = chat(query)
    return {"answer": answer}


@app.get("/search/fulltext", responses={200: {"description": "Successful Response", "content": {"application/json": {"example": {"summary": "Azure OpenAI Service は、OpenAI の強力な言語モデル（GPT-4o、GPT-4 Turbo、GPT-3.5-Turbo など）にアクセスできるサービスです。これにより、コンテンツ生成や自然言語処理、翻訳などのタスクに使用できます。また、REST API、Python SDK、Azure OpenAI Studio を通じて接続が可能です。", "additional_info": "Azure OpenAI では、プロンプトと入力候補を利用して、ユーザーが入力したテキストに応じたテキスト出力を生成します。モデルの設定やデプロイは、Azureのインターフェースを利用して行うことができます。", "source": "出典: [Azure OpenAI Service Documentation]"}}}}})
def search_fulltext_handler(query: str):
    """
    クエリを処理し、書き換え、検索を実行し、回答を生成する
    フルテキスト検索エンドポイント。

    Args:
        query (str): 処理するクエリ文字列。

    Returns:
        dict: クエリに対する回答を含む辞書。
    """
    rewritten_query = rewrite_query(query)
    search_results = search_fulltext(rewritten_query)
    answer = generate_answer(query, search_results)
    return {"answer": answer}


@app.get("/search/vector", responses={200: {"description": "Successful Response", "content": {"application/json": {"example": {"summary": "Azure OpenAI Service は、OpenAI の強力な言語モデル（GPT-4o、GPT-4 Turbo、GPT-3.5-Turbo など）にアクセスできるサービスです。これにより、コンテンツ生成や自然言語処理、翻訳などのタスクに使用できます。また、REST API、Python SDK、Azure OpenAI Studio を通じて接続が可能です。", "additional_info": "Azure OpenAI では、プロンプトと入力候補を利用して、ユーザーが入力したテキストに応じたテキスト出力を生成します。モデルの設定やデプロイは、Azureのインターフェースを利用して行うことができます。", "source": "出典: [Azure OpenAI Service Documentation]"}}}}})
def search_vector_handler(query: str):
    """
    ベクトルを処理し、検索を実行し、回答を生成する
    ベクトル検索エンドポイント。

    Args:
        query (str): 処理するクエリ文字列。

    Returns:
        dict: クエリに対する回答を含む辞書。
    """
    rewritten_query = rewrite_query(query)
    vector = get_embedding_from_query(rewritten_query)
    search_results = search_vector(vector)
    print("search_results", search_results)  # AI Search により返却された回答を確認するために追加
    answer = generate_answer(query, search_results)
    return {"answer": answer}


@app.get("/openapi.json")
def get_open_api_endpoint():
    """
    OpenAPIエンドポイントを取得します。

    このエンドポイントは、Simple APIのOpenAPI仕様を返します。

    Returns:
        dict: OpenAPI仕様。
    """
    return get_openapi(
        title="Simple API",
        version="1.0.0",
        openapi_version="3.0.3",
        description="API for Cloud Workshop Simple App",
        routes=app.routes,
    )


@app.get("/docs", include_in_schema=False)
def get_swagger_documentation():
    """
    Swagger UI ドキュメントを取得します。

    このエンドポイントは、Swagger UI を返します。

    Returns:
        HTMLResponse: Swagger UIのHTMLコンテンツ。
    """
    return get_swagger_ui_html(openapi_url="/openapi.json", title="Simple API Docs")