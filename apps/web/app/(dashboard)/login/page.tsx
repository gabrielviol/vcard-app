"use client";

import { Suspense, useState } from "react";
import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { ApiError, apiFetch } from "@/lib/api-client";
import { setToken } from "@/lib/auth-storage";
import { loginSchema, type LoginFormValues } from "@/lib/auth-schema";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from "@/components/ui/form";
import { Input } from "@/components/ui/input";

type AuthResponse = {
  accessToken: string;
  user: { id: string; email: string };
};

function LoginForm() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const expired = searchParams.get("expired") === "1";
  const [invalidCredentialsError, setInvalidCredentialsError] = useState<string | null>(null);
  const [genericError, setGenericError] = useState<string | null>(null);

  const form = useForm<LoginFormValues>({
    resolver: zodResolver(loginSchema),
    defaultValues: { email: "", password: "" },
  });

  async function onSubmit(values: LoginFormValues) {
    setInvalidCredentialsError(null);
    setGenericError(null);

    try {
      const response = await apiFetch<AuthResponse>("/auth/login", {
        method: "POST",
        auth: false,
        body: JSON.stringify(values),
      });
      setToken(response.accessToken);
      router.push("/dashboard");
    } catch (error) {
      if (error instanceof ApiError && error.status === 401) {
        setInvalidCredentialsError("E-mail ou senha inválidos.");
        return;
      }
      setGenericError("Não foi possível salvar. Verifique sua conexão e tente novamente.");
    }
  }

  return (
    <div className="flex min-h-screen flex-1 items-center justify-center bg-zinc-50 px-4">
      <Card className="w-full max-w-md p-12">
        <CardHeader className="px-0 pb-6">
          <CardTitle className="text-[28px] font-semibold leading-[1.2]">Entrar</CardTitle>
        </CardHeader>
        <CardContent className="px-0">
          {expired && (
            <p className="mb-4 text-sm text-red-600">
              Sua sessão expirou. Entre novamente.
            </p>
          )}
          <Form {...form}>
            <form onSubmit={form.handleSubmit(onSubmit)} className="flex flex-col gap-4">
              <FormField
                control={form.control}
                name="email"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>E-mail</FormLabel>
                    <FormControl>
                      <Input type="email" autoComplete="email" {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
              <FormField
                control={form.control}
                name="password"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Senha</FormLabel>
                    <FormControl>
                      <Input type="password" autoComplete="current-password" {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
              {invalidCredentialsError && (
                <p className="text-sm text-red-600">{invalidCredentialsError}</p>
              )}
              {genericError && <p className="text-sm text-red-600">{genericError}</p>}
              <Button
                type="submit"
                disabled={form.formState.isSubmitting}
                className="mt-2 bg-indigo-600 text-white hover:bg-indigo-700"
              >
                Entrar
              </Button>
              <Button type="button" variant="ghost" asChild>
                <Link href="/register">Criar conta</Link>
              </Button>
            </form>
          </Form>
        </CardContent>
      </Card>
    </div>
  );
}

export default function LoginPage() {
  return (
    <Suspense fallback={null}>
      <LoginForm />
    </Suspense>
  );
}
