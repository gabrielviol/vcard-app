import Link from "next/link";
import { Button } from "@/components/ui/button";
import { PRODUCT_NAME } from "@/lib/brand";

// PUB-06: 404 com a cara do produto, nao a 404 crua do Next.js. Server
// Component simples (roda no servidor, sem props) -- Next.js so aceita
// not-found.js sem props (node_modules/next/dist/docs/.../not-found.md).
//
// D-22: mesmo shell/parentesco visual do cartao publico (public-card-view.tsx)
// para dar familia visual entre o cartao real e o caminho de erro.
// D-23: um unico caminho de aquisicao -- sem link secundario/ghost de "voltar".
//
// T-02-10: a copy e 100% estatica e NUNCA interpola o slug requisitado --
// evita XSS refletido e evita ecoar tentativas de enumeracao de slugs.
export default function NotFound() {
  return (
    <div className="min-h-screen bg-zinc-50">
      <div className="mx-auto flex w-full max-w-[480px] flex-col items-center px-6 py-16">
        <span className="text-sm font-semibold leading-[1.5]">
          {PRODUCT_NAME}
        </span>

        <h1 className="mt-8 text-center text-[28px] font-semibold leading-[1.2]">
          Esse cartão não existe.
        </h1>

        <p className="mt-2 text-center text-base leading-[1.5] text-zinc-600">
          O link que você acessou não corresponde a nenhum cartão publicado.
          Verifique o endereço ou crie o seu.
        </p>

        <Link href="/register" className="mt-8">
          <Button className="bg-indigo-600 text-white hover:bg-indigo-700">
            Criar meu cartão
          </Button>
        </Link>
      </div>
    </div>
  );
}
