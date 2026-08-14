import { handleUpload, type HandleUploadBody } from "@vercel/blob/client";
import { NextResponse } from "next/server";

// Emissão de token de upload do Vercel Blob (CARD-09). Runtime Node (default deste
// Route Handler, NÃO Edge) -- exigido pelo handleUpload do @vercel/blob/client.
//
// A checagem de auth abaixo é a lacuna que a documentação oficial da Vercel não
// cobre (flagged [ASSUMED] em 01-RESEARCH.md/01-PATTERNS.md, T-01-32): sem ela,
// qualquer pessoa na internet conseguiria emitir um token de escrita para o blob
// store do projeto. O Bearer do usuário é validado contra GET /auth/me antes de
// emitir qualquer token de upload.
const ALLOWED_CONTENT_TYPES = ["image/jpeg", "image/png", "image/webp"];
const MAX_UPLOAD_BYTES = 4 * 1024 * 1024; // 4 MB (T-01-33)

export async function POST(request: Request): Promise<NextResponse> {
  const body = (await request.json()) as HandleUploadBody;

  try {
    const jsonResponse = await handleUpload({
      body,
      request,
      onBeforeGenerateToken: async () => {
        const userId = await verifyBearerAndGetUserId(request);

        return {
          allowedContentTypes: ALLOWED_CONTENT_TYPES,
          addRandomSuffix: true,
          maximumSizeInBytes: MAX_UPLOAD_BYTES,
          tokenPayload: JSON.stringify({ userId }),
        };
      },
      onUploadCompleted: async ({ blob }) => {
        // Apenas log -- a persistência de photo_url acontece via PUT /cards/{id}
        // disparado pelo cliente (onUploadCompleted não roda em localhost sem
        // túnel, então não pode ser o único caminho de persistência).
        console.log("[upload] blob concluído:", blob.url);
      },
    });

    return NextResponse.json(jsonResponse);
  } catch (error) {
    if (error instanceof UnauthorizedUploadError) {
      return NextResponse.json({ error: "unauthorized" }, { status: 401 });
    }
    const message = error instanceof Error ? error.message : "Erro desconhecido";
    return NextResponse.json({ error: message }, { status: 400 });
  }
}

class UnauthorizedUploadError extends Error {}

async function verifyBearerAndGetUserId(request: Request): Promise<string> {
  const authHeader = request.headers.get("authorization");
  const token = authHeader?.startsWith("Bearer ")
    ? authHeader.slice("Bearer ".length).trim()
    : null;

  if (!token) {
    throw new UnauthorizedUploadError();
  }

  const apiUrl = process.env.NEXT_PUBLIC_API_URL;
  if (!apiUrl) {
    throw new Error("NEXT_PUBLIC_API_URL not set");
  }

  const meResponse = await fetch(`${apiUrl}/auth/me`, {
    headers: { Authorization: `Bearer ${token}` },
  });

  if (meResponse.status !== 200) {
    throw new UnauthorizedUploadError();
  }

  const me = (await meResponse.json()) as { id: string };
  return me.id;
}
