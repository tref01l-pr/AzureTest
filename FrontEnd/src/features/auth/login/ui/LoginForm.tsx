import { zodResolver } from '@hookform/resolvers/zod'
import { useForm } from 'react-hook-form'
import { getErrorMessage, hasErrorCode } from '@shared/api'
import { Alert, Button, Input } from '@shared/ui'
import { loginSchema, type LoginFormValues } from '../model/login-schema'
import { useLogin } from '../model/use-login'

export function LoginForm({ onSuccess }: { onSuccess: () => void }) {
  const login = useLogin()
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<LoginFormValues>({ resolver: zodResolver(loginSchema) })

  const submit = handleSubmit((values) => login.mutate(values, { onSuccess }))

  return (
    <form onSubmit={submit} noValidate className="flex flex-col gap-4">
      {login.isError && (
        <Alert>
          {hasErrorCode(login.error, 'Auth.InvalidCredentials')
            ? 'Invalid email or password.'
            : getErrorMessage(login.error)}
        </Alert>
      )}
      <Input
        label="Email"
        type="email"
        autoComplete="email"
        error={errors.email?.message}
        {...register('email')}
      />
      <Input
        label="Password"
        type="password"
        autoComplete="current-password"
        error={errors.password?.message}
        {...register('password')}
      />
      <Button type="submit" loading={login.isPending}>
        Sign in
      </Button>
    </form>
  )
}
