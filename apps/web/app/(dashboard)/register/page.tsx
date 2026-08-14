"use client";

import { useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { ApiError, apiFetch } from "@/lib/api-client";
import { setToken } from "@/lib/auth-storage";
import { registerSchema, type RegisterFormValues } from "@/lib/auth-schema";
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

export default function RegisterPage() {
  const router = useRouter();
  const [emailTakenError, setEmailTakenError] = useState<string | null>(null);
  const [genericError, setGenericError] = useState<string | null>(null);

  const form = useForm<RegisterFormValues>({
    resolver: zodResolver(registerSchema),
    defaultValues: { email: "", password: "" },
  });

  async function onSubmit(values: RegisterFormValues) {
    setEmailTakenError(null);
    setGenericError(null);

    try {
      const response = await apiFetch<AuthResponse>("/auth/register", {
        method: "POST",
        auth: false,
        body: JSON.stringify(values),
      });
      setToken(response.accessToken);
      router.push("/dashboard");
    } catch (error) {
      if (error instanceof ApiError && error.status === 409 && error.code === "email_taken") {
        setEmailTakenError("Esse e-mail já está cadastrado. Entre na sua conta.");
        return;
      }
      setGenericError("Não foi possível salvar. Verifique sua conexão e tente novamente.");
    }
  }

  return (
    <div className="flex min-h-screen flex-1 items-center justify-center bg-zinc-50 px-4">
      <Card className="w-full max-w-md p-12">
        <CardHeader className="px-0 pb-6">
          <CardTitle className="text-[28px] font-semibold leading-[1.2]">
            Criar conta
          </CardTitle>
        </CardHeader>
        <CardContent className="px-0">
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
                    {emailTakenError && (
                      <p className="text-sm text-red-600">{emailTakenError}</p>
                    )}
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
                      <Input type="password" autoComplete="new-password" {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
              {genericError && <p className="text-sm text-red-600">{genericError}</p>}
              <Button
                type="submit"
                disabled={form.formState.isSubmitting}
                className="mt-2 bg-indigo-600 text-white hover:bg-indigo-700"
              >
                Criar conta
              </Button>
              <Button type="button" variant="ghost" asChild>
                <Link href="/login">Já tenho conta</Link>
              </Button>
            </form>
          </Form>
        </CardContent>
      </Card>
    </div>
  );
}
