import { Center, Stack } from '@mantine/core'
import { FC } from 'react'
import { PropsWithChildren } from 'react'
import { useNavigate } from 'react-router'
import { LogoHeader } from '@Components/LogoHeader'
import misc from '@Styles/Misc.module.css'

interface AccountViewProps extends PropsWithChildren {
  onSubmit?: (event: React.FormEvent) => Promise<void>
}

export const AccountView: FC<AccountViewProps> = ({ onSubmit, children }) => {
  const navigate = useNavigate()

  return (
    <Center h="100vh">
      <Stack align="center" justify="center">
        <LogoHeader onClick={() => navigate('/')} />
        <form className={misc.accountForm} onSubmit={onSubmit}>
          <Stack gap="xs" align="center" justify="center">
            {children}
          </Stack>
        </form>
      </Stack>
    </Center>
  )
}
