import { AppShell, Box, LoadingOverlay, Stack, useMantineColorScheme, useMantineTheme } from '@mantine/core'
import React, { FC, useState } from 'react'
import { AppFooter } from '@Components/AppFooter'
import { AppHeader } from '@Components/AppHeader'
import { AppNavbar } from '@Components/AppNavbar'
import { CustomColorModal } from '@Components/CustomColorModal'
import { IconHeader } from '@Components/IconHeader'
import { Watermark } from '@Components/Watermark'
import { WithWiderScreen } from '@Components/WithWiderScreen'
import { DEFAULT_LOADING_OVERLAY } from '@Utils/Shared'
import { useIsMobile } from '@Utils/ThemeOverride'
import { useUser } from '@Hooks/useUser'
import classes from '@Styles/AppNavbar.module.css'

interface WithNavBarProps extends React.PropsWithChildren {
  width?: string
  minWidth?: number
  isLoading?: boolean
  withFooter?: boolean
  withHeader?: boolean
  stickyHeader?: boolean
}

export interface AppControlProps {
  openColorModal: () => void
}

export const WithNavBar: FC<WithNavBarProps> = ({
  children,
  width,
  isLoading,
  minWidth,
  withFooter = false,
  withHeader,
  stickyHeader = false,
}) => {
  const theme = useMantineTheme()
  const { colorScheme } = useMantineColorScheme()
  const { user } = useUser()
  const isMobile = useIsMobile()
  const [colorModalOpened, setColorModalOpened] = useState(false)

  const openColorModal = () => setColorModalOpened(true)

  return (
    <WithWiderScreen minWidth={minWidth}>
      <Watermark
        text={user?.userId ?? ''}
        textColor={colorScheme === 'dark' ? theme.colors.gray[3] : theme.colors.gray[7]}
        rotate={-12}
        textSize={14}
        gutter={22}
        opacity={colorScheme === 'dark' ? 0.018 : 0.025}
      >
        <AppShell
          p={0}
          header={{ height: 60, collapsed: !isMobile }}
          navbar={{
            width: 65,
            breakpoint: 'sm',
            collapsed: {
              mobile: true,
            },
          }}
        >
          <AppHeader openColorModal={openColorModal} />
          <AppNavbar openColorModal={openColorModal} />
          <AppShell.Main w="100%">
            <Stack data-mobile={isMobile || undefined} data-pb={withFooter || undefined} className={classes.main}>
              <LoadingOverlay visible={isLoading ?? false} overlayProps={DEFAULT_LOADING_OVERLAY} />
              {withHeader && <IconHeader px={isMobile ? '2%' : '10%'} sticky={stickyHeader} />}
              <Box
                w={width ?? (isMobile ? '96%' : '80%')}
                style={{
                  zIndex: 20,
                }}
              >
                {children}
              </Box>
              <CustomColorModal opened={colorModalOpened} onClose={() => setColorModalOpened(false)} />
            </Stack>
            {withFooter && <AppFooter />}
          </AppShell.Main>
        </AppShell>
      </Watermark>
    </WithWiderScreen>
  )
}
