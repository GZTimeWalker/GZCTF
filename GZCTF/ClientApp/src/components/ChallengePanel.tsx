import React, { FC } from 'react'
import { useParams } from 'react-router-dom'
import { Group, ScrollArea, SimpleGrid, Tabs, Text } from '@mantine/core'
import { Icon } from '@mdi/react'
import api, { ChallengeTag } from '../Api'
import { ChallengeTagLabelMap } from './ChallengeItem'

const ChallengePanel: FC = () => {
  const { id } = useParams()
  const numId = parseInt(id ?? '-1')

  const { data: challenges } = api.game.useGameChallenges(numId)

  const tags = Object.keys(challenges ?? {})
  const [activeTab, setActiveTab] = React.useState<ChallengeTag>(tags[0] as ChallengeTag)

  return (
    <Group noWrap position="apart" align="flex-start" style={{ width: 'calc(100% - 21rem)' }}>
      <Tabs
        orientation="vertical"
        variant="pills"
        value={activeTab}
        onTabChange={(value) => setActiveTab(value as ChallengeTag)}
        styles={{
          tabsList: {
            minWidth: '10rem',
          },
          tab: {
            fontWeight: 700,
          },
          tabLabel: {
            width: '100%',
          },
        }}
      >
        <Tabs.List>
          {Object.keys(challenges ?? {}).map((tab) => {
            const data = ChallengeTagLabelMap.get(tab as ChallengeTag)!
            return (
              <Tabs.Tab value={tab} icon={<Icon path={data?.icon} size={1} />} color={data?.color}>
                <Group position="apart">
                  <Text>{data?.label}</Text>
                  <Text>{challenges && challenges[tab].length}</Text>
                </Group>
              </Tabs.Tab>
            )
          })}
        </Tabs.List>
      </Tabs>
      <ScrollArea
        style={{
          width: 'calc(100% - 10rem)',
          height: 'calc(100vh - 100px)',
          position: 'relative',
        }}
      >
        <SimpleGrid cols={3}>
          Hello
        </SimpleGrid>
      </ScrollArea>
    </Group>
  )
}

export default ChallengePanel
