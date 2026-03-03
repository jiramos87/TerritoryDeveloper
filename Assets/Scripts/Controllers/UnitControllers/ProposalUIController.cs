using UnityEngine;
using UnityEngine.UI;

public class ProposalUIController : MonoBehaviour
{
    public UrbanizationProposal proposal;
    public UrbanizationProposalManager manager;
    public Text nameLabel;
    public Text costLabel;
    public Button acceptButton;
    public Button rejectButton;

    public void SetProposal(UrbanizationProposal p, UrbanizationProposalManager m)
    {
        proposal = p;
        manager = m;
        if (nameLabel != null && p != null) nameLabel.text = p.proposalName;
        if (costLabel != null && p != null) costLabel.text = "$" + p.totalCost;
        if (acceptButton != null) acceptButton.onClick.RemoveAllListeners();
        if (acceptButton != null) acceptButton.onClick.AddListener(OnAccept);
        if (rejectButton != null) rejectButton.onClick.RemoveAllListeners();
        if (rejectButton != null) rejectButton.onClick.AddListener(OnReject);
    }

    void OnAccept()
    {
        if (manager != null && proposal != null)
            manager.AcceptProposal(proposal);
    }

    void OnReject()
    {
        if (manager != null && proposal != null)
            manager.RejectProposal(proposal);
    }
}
