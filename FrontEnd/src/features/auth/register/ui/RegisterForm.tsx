import { zodResolver } from '@hookform/resolvers/zod'
import { useForm } from 'react-hook-form'
import { getErrorMessage } from '@shared/api'
import { Alert, Button, Input } from '@shared/ui'
import { registerSchema, type RegisterFormValues } from '../model/register-schema'
import { useRegister } from '../model/use-register'

export function RegisterForm({ onSuccess }: { onSuccess: () => void }) {
  const registration = useRegister()
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<RegisterFormValues>({ resolver: zodResolver(registerSchema) })

  const submit = handleSubmit(({ email, password, confirmPassword }) =>
    registration.mutate({ email, password, confirmPassword }, { onSuccess }),
  )

  return (
    <form onSubmit={submit} noValidate className="flex flex-col gap-4">
      {registration.isError && <Alert>{getErrorMessage(registration.error)}</Alert>}
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
        autoComplete="new-password"
        hint="8+ characters with upper- and lowercase letters and a digit"
        error={errors.password?.message}
        {...register('password')}
      />
      <Input
        label="Confirm password"
        type="password"
        autoComplete="new-password"
        error={errors.confirmPassword?.message}
        {...register('confirmPassword')}
      />
      <Button type="submit" loading={registration.isPending}>
        Create account
      </Button>
    </form>
  )
}
