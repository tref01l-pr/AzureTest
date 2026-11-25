import { FiLogOut } from 'react-icons/fi'
import { Button } from '@shared/ui'
import { useLogout } from '../model/use-logout'

export function LogoutButton() {
  const logout = useLogout()
  return (
    <Button variant="ghost" size="sm" loading={logout.isPending} onClick={() => logout.mutate()}>
      <FiLogOut aria-hidden="true" /> Log out
    </Button>
  )
}
