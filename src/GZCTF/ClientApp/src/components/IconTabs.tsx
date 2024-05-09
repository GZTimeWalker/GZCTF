import { Box, Group, GroupProps, MantineColor } from '@mantine/core'
import { createStyles, getStylesRef } from '@mantine/emotion'
import { clamp } from '@mantine/hooks'
import React, { FC, useEffect, useState } from 'react'
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
  position?: React.CSSProperties['justifyContent']
  tabs: TabProps[]
  grow?: boolean
  active?: number
  withIcon?: boolean
  aside?: React.ReactNode
  onTabChange?: (tabIndex: number, tabKey: string) => void
}

const useTabStyle = createStyles((theme, props: TabStyleProps, u) => {
  const activeTab = { ref: getStylesRef('activeTab') } as const
  const _color = props.color ?? theme.primaryColor

  return {
    activeTab,
    default: {
      transition: 'border-color 100ms ease, color 100ms ease, background 100ms ease',
      borderRadius: theme.radius.sm,
      fontSize: theme.fontSizes.sm,
      height: 'auto',
      padding: `${theme.spacing.xs} ${theme.spacing.lg}`,
      fontWeight: 500,
      boxSizing: 'border-box',
      cursor: 'pointer',
      border: 0,
      display: 'block',
      backgroundColor: 'transparent',

      [u.smallerThan('xs')]: {
        padding: `${theme.spacing.xs} ${theme.spacing.md}`,
      },

      [u.dark]: {
        color: theme.colors.dark[1],
      },

      [u.light]: {
        color: theme.colors.gray[7],
      },

      '&:disabled': {
        cursor: 'not-allowed',

        [u.dark]: {
          color: theme.colors.dark[3],
        },

        [u.light]: {
          color: theme.colors.gray[5],
        },
      },

      '&:hover': {
        [u.dark]: {
          backgroundColor: theme.colors.dark[6],
        },

        [u.light]: {
          backgroundColor: theme.colors.gray[0],
        },
      },

      [`&.${activeTab.ref}`]: {
        [u.dark]: {
          backgroundColor: theme.colors.dark[7],
          color: theme.colors[_color][4],
        },

        [u.light]: {
          backgroundColor: theme.white,
          color: theme.colors[_color][6],
        },
      },
    },
    tabInner: {
      boxSizing: 'border-box',
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'center',
      lineHeight: 1,
      height: '100%',

      [u.smallerThan('xs')]: {
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
    <Group gap={0} justify="space-between" w="100%" wrap="nowrap">
      {aside}
      {withIcon && (
        <LogoHeader
          sx={(_, u) => ({
            [u.smallerThan('xs')]: {
              display: 'none',
            },
          })}
        />
      )}
      <Group
        justify="right"
        wrap="nowrap"
        gap={5}
        sx={(_, u) => ({
          [u.smallerThan('xs')]: {
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
