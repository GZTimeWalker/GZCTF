import { Center, Stack } from '@mantine/core'
import { FC } from 'react'
import { PropsWithChildren } from 'react'
import { useNavigate } from 'react-router-dom'
import LogoHeader from '@Components/LogoHeader'

interface AccountViewProps extends PropsWithChildren {
  onSubmit?: (event: React.FormEvent) => Promise<void>
}

const AccountView: FC<AccountViewProps> = ({ onSubmit, children }) => {
  const navigate = useNavigate()

  return (
    <Center h="100vh">
      <Stack align="center" justify="center">
        <LogoHeader onClick={() => navigate('/')} />
        <form
          style={{
            width: '300px',
            minWidth: '300px',
            maxWidth: '300px',
          }}
          onSubmit={onSubmit}
        >
          <Stack gap="xs" align="center" justify="center">
            {children}
          </Stack>
        </form>
      </Stack>
    </Center>
  )
}

export default AccountView
