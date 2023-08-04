import React, { FC, useEffect, useState } from 'react'
import {
  Box,
  createStyles,
  getStylesRef,
  Group,
  GroupPosition,
  GroupProps,
  MantineColor,
} from '@mantine/core'
import { clamp } from '@mantine/hooks'
import LogoHeader from '@Components/LogoHeader'

interface TabStyleProps {
  color?: MantineColor
}

interface TabProps {
  tabKey: string
  color?: MantineColor
  icon?: React.ReactNode
  label?: React.ReactNode
}

interface IconTabsProps extends GroupProps {
  position?: GroupPosition
  tabs: TabProps[]
  grow?: boolean
  active?: number
  withIcon?: boolean
  aside?: React.ReactNode
  onTabChange?: (tabIndex: number, tabKey: string) => void
}

const useTabStyle = createStyles((theme, props: TabStyleProps) => {
  const activeTab = { ref: getStylesRef('activeTab') } as const
  const color = props.color ?? 'brand'

  return {
    activeTab,
    default: {
      transition: 'border-color 100ms ease, color 100ms ease, background 100ms ease',
      borderRadius: theme.radius.sm,
      color: theme.colorScheme === 'dark' ? theme.colors.dark[1] : theme.colors.gray[7],
      fontSize: theme.fontSizes.sm,
      height: 'auto',
      padding: `${theme.spacing.xs} ${theme.spacing.lg}`,
      fontWeight: 500,
      boxSizing: 'border-box',
      cursor: 'pointer',
      border: 0,
      display: 'block',
      backgroundColor: 'transparent',

      [theme.fn.smallerThan('xs')]: {
        padding: `${theme.spacing.xs} ${theme.spacing.md}`,
      },

      '&:disabled': {
        cursor: 'not-allowed',
        color: theme.colorScheme === 'dark' ? theme.colors.dark[3] : theme.colors.gray[5],
      },

      '&:hover': {
        background: theme.colorScheme === 'dark' ? theme.colors.dark[6] : theme.colors.gray[0],
      },

      [`&.${activeTab.ref}`]: {
        color: theme.fn.themeColor(color as string, theme.colorScheme === 'dark' ? 4 : 6),
        background: theme.colorScheme === 'dark' ? theme.colors.dark[7] : theme.white,
      },
    },
    tabInner: {
      boxSizing: 'border-box',
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'center',
      lineHeight: 1,
      height: '100%',

      [theme.fn.smallerThan('xs')]: {
        flexDirection: 'column',
        gap: theme.spacing.md,
      },
    },
    tabLabel: {
      fontWeight: 700,
    },
    tabIcon: {
      margin: 'auto',

      '&:not(:only-child)': {
        marginRight: theme.spacing.xs,
      },
      '& *': {
        display: 'block',
      },
    },
  }
})

const Tab: FC<TabProps & { active: boolean; onClick?: () => void }> = (props) => {
  const { color, label, active, icon, tabKey, ...others } = props
  const { classes, cx } = useTabStyle({ color })

  return (
    <Box
      {...others}
      component="button"
      type="button"
      role="tab"
      key={tabKey}
      className={cx(classes.default, { [classes.activeTab]: active })}
    >
      <div className={classes.tabInner}>
        {icon && <div className={classes.tabIcon}>{icon}</div>}
        {label && <div className={classes.tabLabel}>{label}</div>}
      </div>
    </Box>
  )
}

const IconTabs: FC<IconTabsProps> = (props) => {
  const { active, onTabChange, tabs, withIcon, aside, ...others } = props
  const [_activeTab, setActiveTab] = useState(active ?? 0)

  const activeTab = clamp(_activeTab, 0, tabs.length - 1)

  useEffect(() => {
    setActiveTab(active ?? 0)
  }, [active])

  const panes = tabs.map((tab, index) => (
    <Tab
      {...tab}
      key={tab.tabKey}
      active={activeTab === index}
      onClick={() => {
        setActiveTab(index)
        onTabChange && onTabChange(index, tab.tabKey)
      }}
    />
  ))

  return (
    <Group spacing={0} position="apart" w="100%" noWrap>
      {aside}
      {withIcon && (
        <LogoHeader
          sx={(theme) => ({
            [theme.fn.smallerThan('xs')]: {
              display: 'none',
            },
          })}
        />
      )}
      <Group
        position="right"
        noWrap
        spacing={5}
        sx={(theme) => ({
          [theme.fn.smallerThan('xs')]: {
            width: '100%',
            justifyContent: 'space-around',
          },
        })}
        {...others}
      >
        {panes}
      </Group>
    </Group>
  )
}

export default IconTabs
