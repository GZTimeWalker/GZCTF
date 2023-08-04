import { FC } from 'react'
import { useNavigate } from 'react-router-dom'
import { Stack, Center, createStyles } from '@mantine/core'
import LogoHeader from '@Components/LogoHeader'

const useStyles = createStyles(() => ({
  input: {
    width: '25vw',
    minWidth: '250px',
    maxWidth: '300px',
  },
}))

interface AccountViewProps extends React.PropsWithChildren {
  onSubmit?: (event: React.FormEvent) => Promise<void>
}

const AccountView: FC<AccountViewProps> = ({ onSubmit, children }) => {
  const { classes } = useStyles()
  const navigate = useNavigate()

  return (
    <Center h="100vh">
      <Stack align="center" justify="center">
        <LogoHeader onClick={() => navigate('/')} />
        <form className={classes.input} onSubmit={onSubmit}>
          <Stack spacing="xs" align="center" justify="center">
            {children}
          </Stack>
        </form>
      </Stack>
    </Center>
  )
}

export default AccountView
