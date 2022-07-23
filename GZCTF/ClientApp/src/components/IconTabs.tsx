import React, { FC, useState } from 'react'
import { Box, createStyles, Group, GroupPosition, MantineColor } from '@mantine/core'
import { clamp } from '@mantine/hooks'
import LogoHeader from './LogoHeader'

interface TabStyleProps {
  color?: MantineColor
}

interface TabProps {
  tabKey: string
  color?: MantineColor
  icon?: React.ReactNode
  label?: React.ReactNode
}

interface IconTabsProps {
  position?: GroupPosition
  tabs: TabProps[]
  grow?: boolean
  active?: number
  onTabChange?: (tabIndex: number, tabKey: string) => void
}

const useTabStyle = createStyles((theme, props: TabStyleProps, getRef) => {
  const activeTab = { ref: getRef('activeTab') } as const
  const color = props.color ?? 'brand'

  return {
    activeTab,
    default: {
      transition: 'border-color 100ms ease, color 100ms ease, background 100ms ease',
      borderRadius: theme.radius.sm,
      color: theme.colorScheme === 'dark' ? theme.colors.dark[1] : theme.colors.gray[7],
      fontSize: theme.fontSizes.sm,
      height: 'auto',
      padding: `${theme.spacing.xs}px ${theme.spacing.lg}px`,
      fontWeight: 500,
      boxSizing: 'border-box',
      cursor: 'pointer',
      display: 'block',
      border: 0,
      backgroundColor: 'transparent',

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
    },
    tabLabel: {},
    tabIcon: {
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
  const { active, onTabChange, tabs, ...others } = props
  const [_activeTab, setActiveTab] = useState(active ?? 0)

  const activeTab = clamp({ value: _activeTab, min: 0, max: tabs.length - 1 })

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
    <Group position="apart" style={{ width: '100%' }}>
      <LogoHeader />
      <Group position="right" spacing={5} {...others}>
        {panes}
      </Group>
    </Group>
  )
}

export default IconTabs
