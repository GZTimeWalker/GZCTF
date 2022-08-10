import { FC } from 'react'
import { Stack, Center, createStyles } from '@mantine/core'
import LogoHeader from './LogoHeader'

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

  return (
    <Center style={{ height: '100vh' }}>
      <Stack align="center" justify="center">
        <LogoHeader />
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
